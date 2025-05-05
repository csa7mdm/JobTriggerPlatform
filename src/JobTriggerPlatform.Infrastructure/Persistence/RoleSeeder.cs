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
        /// Developer role with access to deployment and job configurations.
        /// </summary>
        public const string Dev = "Dev";
        
        /// <summary>
        /// Quality Assurance role with access to trigger jobs and view results.
        /// </summary>
        public const string QA = "QA";
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
        await CreateRoleIfNotExistsAsync(roleManager, Roles.Dev, "Access to deployment configuration and management");
        await CreateRoleIfNotExistsAsync(roleManager, Roles.QA, "Access to trigger jobs and view results");
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
