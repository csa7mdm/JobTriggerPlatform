using JobTriggerPlatform.Application.Abstractions;
using JobTriggerPlatform.Application.Interfaces;
using JobTriggerPlatform.Domain.Identity;
using JobTriggerPlatform.Infrastructure.Authorization;
using JobTriggerPlatform.Infrastructure.Email;
using JobTriggerPlatform.Infrastructure.Persistence;
using JobTriggerPlatform.Infrastructure.Plugins;
using JobTriggerPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using System.Net;

namespace JobTriggerPlatform.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring infrastructure services.
    /// </summary>
    public static class InfrastructureServiceCollectionExtensions
    {
        /// <summary>
        /// Adds infrastructure services to the specified service collection.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="pluginsPath">Optional custom path to the plugins directory.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration,
            string? pluginsPath = null)
        {
            // Configure PostgreSQL DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            // Register Identity with complete services (including RoleManager)
            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
                {
                // Configure identity options based on environment
                if (configuration.GetValue<bool>("UseStrictPasswordPolicy", true))
                {
                    // Standard password settings
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequiredLength = 8;
                }
                else
                {
                    // Relaxed password settings for development
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 6;
                }

                    // Lockout settings
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                    options.Lockout.MaxFailedAccessAttempts = 5;

                    // User settings
                    options.User.RequireUniqueEmail = true;

                    // Email confirmation settings
                    options.SignIn.RequireConfirmedEmail = false;

                    // Two-factor authentication settings
                    options.SignIn.RequireConfirmedAccount = false;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .AddRoleManager<RoleManager<ApplicationRole>>();
                
            // Ensure RoleManager is explicitly registered as a fallback
            services.AddScoped<RoleManager<ApplicationRole>>();

            // Register Email Service
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.AddScoped<IEmailService, EmailService>();

            // Register Jenkins Service with HttpClientFactory and standard resilience handler
            services.AddHttpClient("JenkinsClient")
                .AddStandardResilienceHandler(options =>
                {
                    // Configure the standard resilience options
                    options.Retry.MaxRetryAttempts = 3;
                    options.Retry.BackoffType = DelayBackoffType.Exponential;
                    options.Retry.UseJitter = true;

                    // Configure the transient fault detection for retry
                    options.Retry.ShouldHandle = args =>
                    {
                        // Handle HTTP request exceptions
                        if (args.Outcome.Exception is HttpRequestException)
                            return ValueTask.FromResult(true);

                        // Handle specific HTTP status codes
                        if (args.Outcome.Result?.StatusCode >= HttpStatusCode.InternalServerError ||
                            args.Outcome.Result?.StatusCode == HttpStatusCode.RequestTimeout)
                            return ValueTask.FromResult(true);

                        return ValueTask.FromResult(false);
                    };
                });

            services.AddScoped<IJenkinsService, JenkinsService>();

            // Add job access authorization
            services.AddJobAccessPolicies();

            // Add plugins
            services.AddPlugins(pluginsPath);

            return services;
        }
    }
}