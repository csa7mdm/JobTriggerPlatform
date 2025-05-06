using JobTriggerPlatform.Application.Abstractions;
using JobTriggerPlatform.Application.Interfaces;
using JobTriggerPlatform.Domain.Identity;
using JobTriggerPlatform.Infrastructure.Authorization;
using JobTriggerPlatform.Infrastructure.Email;
using JobTriggerPlatform.Infrastructure.Persistence;
using JobTriggerPlatform.Infrastructure.Plugins;
using JobTriggerPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

            // Register Identity
            services.AddIdentityCore<ApplicationUser>(options =>
                {
                    // Password settings
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequiredLength = 8;

                    // Lockout settings
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                    options.Lockout.MaxFailedAccessAttempts = 5;

                    // User settings
                    options.User.RequireUniqueEmail = true;

                    // Email confirmation settings
                    options.SignIn.RequireConfirmedEmail = true;

                    // Two-factor authentication settings
                    options.SignIn.RequireConfirmedAccount = true;
                    options.SignIn.RequireConfirmedEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

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