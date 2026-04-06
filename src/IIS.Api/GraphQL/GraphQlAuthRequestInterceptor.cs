using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace IIS.Api.GraphQL;

/// <summary>
/// Ensures the JWT bearer scheme is evaluated before Hot Chocolate authorization so
/// <see cref="HttpContext.User"/> matches ASP.NET Core authentication (fixes empty principal on some hosts).
/// </summary>
public sealed class GraphQlAuthRequestInterceptor(IPolicyEvaluator policyEvaluator, IAuthorizationPolicyProvider policyProvider)
    : DefaultHttpRequestInterceptor
{
    public override async ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var policy = await policyProvider.GetDefaultPolicyAsync().ConfigureAwait(false);
        await policyEvaluator.AuthenticateAsync(policy, context).ConfigureAwait(false);
        await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken).ConfigureAwait(false);
    }
}
