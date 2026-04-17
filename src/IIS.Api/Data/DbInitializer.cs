using IIS.Api.Entities;
using Microsoft.AspNetCore.Identity;

namespace IIS.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { "Reader", "Full" })
        {
            if (!await roleManager.RoleExistsAsync(role).ConfigureAwait(false))
                await roleManager.CreateAsync(new IdentityRole(role)).ConfigureAwait(false);
        }

        await EnsureUserAsync(userManager, "reader", "Reader1", "Reader").ConfigureAwait(false);
        await EnsureUserAsync(userManager, "full", "Full123", "Full").ConfigureAwait(false);
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> users, string username, string password, string role)
    {
        var existing = await users.FindByNameAsync(username).ConfigureAwait(false);
        if (existing != null)
            return;
        var user = new ApplicationUser { UserName = username };
        var result = await users.CreateAsync(user, password).ConfigureAwait(false);
        if (result.Succeeded)
            await users.AddToRoleAsync(user, role).ConfigureAwait(false);
    }
}
