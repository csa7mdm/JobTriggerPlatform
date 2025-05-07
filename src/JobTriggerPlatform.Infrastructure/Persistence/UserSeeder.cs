using JobTriggerPlatform.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace JobTriggerPlatform.Infrastructure.Persistence;

/// <summary>
/// Seeds predefined users in the database.
/// </summary>
public class UserSeeder
{
    /// <summary>
    /// Predefined user names.
    /// </summary>
    public static class Users
    {
        /// <summary>
        /// Administrator user with full access to the platform.
        /// </summary>
        public const string Admin = "admin@example.com";
        
        /// <summary>
        /// Operator user with access to deployment operations.
        /// </summary>
        public const string Operator = "operator@example.com";
        
        /// <summary>
        /// Viewer user with read-only access.
        /// </summary>
        public const string Viewer = "viewer@example.com";
    }

    /// <summary>
    /// Seeds the predefined users in the database.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SeedUsersAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<UserSeeder>>();

        logger.LogInformation("Seeding users");

        // Seed admin user
        await CreateUserIfNotExistsAsync(
            userManager,
            roleManager,
            Users.Admin,
            "Admin User",
            "Password123!",
            new[] { RoleSeeder.Roles.Admin },
            logger);

        // Seed operator user
        await CreateUserIfNotExistsAsync(
            userManager,
            roleManager,
            Users.Operator,
            "Operator User",
            "Password123!",
            new[] { RoleSeeder.Roles.Operator },
            logger);

        // Seed viewer user
        await CreateUserIfNotExistsAsync(
            userManager,
            roleManager,
            Users.Viewer,
            "Viewer User",
            "Password123!",
            new[] { RoleSeeder.Roles.Viewer },
            logger);
    }

    private static async Task CreateUserIfNotExistsAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        string email,
        string fullName,
        string password,
        string[] roles,
        ILogger logger)
    {
        var user = await userManager.FindByEmailAsync(email);
        
        if (user == null)
        {
            logger.LogInformation("Creating user {Email}", email);
            
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true, // Skip email confirmation for seed users
                FullName = fullName
            };

            var result = await userManager.CreateAsync(user, password);
            
            if (result.Succeeded)
            {
                logger.LogInformation("User {Email} created successfully", email);
                
                // Ensure the user's email is confirmed
                if (!user.EmailConfirmed)
                {
                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    await userManager.ConfirmEmailAsync(user, token);
                    logger.LogInformation("Email confirmed for user {Email}", email);
                }
                
                // Assign roles
                foreach (var role in roles)
                {
                    // Ensure role exists
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        logger.LogWarning("Role {Role} does not exist, skipping assignment", role);
                        continue;
                    }
                    
                    result = await userManager.AddToRoleAsync(user, role);
                    
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Added user {Email} to role {Role}", email, role);
                    }
                    else
                    {
                        logger.LogWarning("Failed to add user {Email} to role {Role}: {Errors}",
                            email, role, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
            else
            {
                logger.LogWarning("Failed to create user {Email}: {Errors}",
                    email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogInformation("User {Email} already exists", email);
        }
    }
}