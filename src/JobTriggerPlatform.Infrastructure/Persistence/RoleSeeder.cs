using JobTriggerPlatform.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobTriggerPlatform.Infrastructure.Persistence;

/// <summary>
/// Seeds predefined roles in the database.
/// </summary>
public class RoleSeeder
{
    /// <summary>
    /// Predefined role names.
    /// </summary>
    public static class Roles
    {
        /// <summary>
        /// Administrator role with full access to the platform.
        /// </summary>
        public const string Admin = "Admin";
        
        /// <summary>
        /// Operator role with permissions to perform deployment operations.
        /// </summary>
        public const string Operator = "Operator";
        
        /// <summary>
        /// Viewer role with read-only access to the platform.
        /// </summary>
        public const string Viewer = "Viewer";
    }

    /// <summary>
    /// Seeds the predefined roles in the database.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<RoleSeeder>>();

        logger.LogInformation("Seeding roles");

        await CreateRoleIfNotExistsAsync(roleManager, Roles.Admin, "Full access to all platform features");
        await CreateRoleIfNotExistsAsync(roleManager, Roles.Operator, "Access to deployment operations");
        await CreateRoleIfNotExistsAsync(roleManager, Roles.Viewer, "Read-only access to view deployment jobs");
    }

    private static async Task CreateRoleIfNotExistsAsync(RoleManager<ApplicationRole> roleManager, string roleName, string description)
    {
        var roleExists = await roleManager.RoleExistsAsync(roleName);
        if (!roleExists)
        {
            var role = new ApplicationRole(roleName, description);
            await roleManager.CreateAsync(role);
        }
    }
}
