using System.Security.Claims;
using System.Text;
using IIS.Api.Data;
using IIS.Api.Entities;
using IIS.Api.GraphQL;
using IIS.Api.Grpc;
using IIS.Api.Options;
using IIS.Api.Services;
using IIS.Api.Soap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Builder;
using SoapCore;

var builder = WebApplication.CreateBuilder(args);

// gRPC requires HTTP/2. Without TLS, Kestrel only enables cleartext HTTP/2 (h2c) on endpoints
// configured as Http2 — Http1AndHttp2 on HTTP falls back to HTTP/1.1 only (see Kestrel warning 64).
// REST/GraphQL/SOAP on 5136 (HTTP/1.1); gRPC on 5137 (HTTP/2 cleartext).
builder.WebHost.ConfigureKestrel(o =>
{
    o.ListenLocalhost(5136, lo => lo.Protocols = HttpProtocols.Http1);
    o.ListenLocalhost(5137, lo => lo.Protocols = HttpProtocols.Http2);
});

builder.Services.Configure<TaskApiOptions>(builder.Configuration.GetSection(TaskApiOptions.SectionName));

builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddIdentityCore<ApplicationUser>(o =>
    {
        o.Password.RequireDigit = false;
        o.Password.RequiredLength = 1;
        o.Password.RequireLowercase = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequiredUniqueChars = 1;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(o =>
    {
        o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Email
        };
    });

builder.Services.AddAuthorization(o =>
{
    o.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
    o.AddPolicy("FullOnly", p => p
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .RequireRole("Full"));
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddGrpc();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<PublicTaskApiClient>();

builder.Services.AddScoped<XsdXmlValidator>();
builder.Services.AddSingleton<JsonSchemaImportValidator>();
builder.Services.AddScoped<CustomTaskStore>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<ITaskOperations, TaskOperationsFacade>();
builder.Services.AddScoped<ITaskSearchSoap, TaskSearchSoapService>();
builder.Services.AddSingleton<WeatherGrpcService>();

builder.Services.AddGraphQLServer()
    .AddQueryType<TaskQueries>()
    .AddMutationType<TaskMutations>()
    .AddAuthorization()
    .AddHttpRequestInterceptor<GraphQlAuthRequestInterceptor>();

builder.Services.AddCors(o =>
{
    o.AddPolicy("BlazorClient", p =>
        p.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5147"])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync().ConfigureAwait(false);
    await DbInitializer.SeedAsync(scope.ServiceProvider).ConfigureAwait(false);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("BlazorClient");
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/soap/TaskSearch.svc", StringComparison.OrdinalIgnoreCase))
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!context.User.IsInRole("Full"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
    }

    await next().ConfigureAwait(false);
});

app.MapControllers();
app.MapGraphQL("/graphql").RequireAuthorization();

app.MapGrpcService<WeatherGrpcService>().RequireAuthorization("FullOnly");
((IApplicationBuilder)app).UseSoapEndpoint<ITaskSearchSoap>(
    "/soap/TaskSearch.svc",
    new SoapEncoderOptions(),
    SoapSerializer.DataContractSerializer);

app.Run();
