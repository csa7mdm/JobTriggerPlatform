using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JobTriggerPlatform.Infrastructure.Persistence
{
    /// <summary>
    /// Handles database initialization, including migrations and initial data seeding.
    /// </summary>
    public static class DatabaseInitializer
    {
        /// <summary>
        /// Initializes the database by applying migrations and seeding initial data.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                
                // Ensure schema exists
                await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS identity;");
                
                // Apply migrations
                logger.LogInformation("Applying database migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully.");
                
                // Database is now ready for seeding roles and users
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }
        }
    }
}
