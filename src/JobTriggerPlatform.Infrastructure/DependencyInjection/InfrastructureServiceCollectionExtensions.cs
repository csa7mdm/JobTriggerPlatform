using JobTriggerPlatform.Application.Interfaces;
using JobTriggerPlatform.Application.Abstractions;
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
using Polly.Extensions.Http;
using System.Net;

namespace JobTriggerPlatform.Infrastructure.DependencyInjection;

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
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
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

        // Register Jenkins Service with HttpClientFactory and Polly retry policy
        services.AddHttpClient("JenkinsClient")
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddScoped<IJenkinsService, JenkinsService>();

        // Add job access authorization
        services.AddJobAccessPolicies();

        // Add plugins
        services.AddPlugins(pluginsPath);

        return services;
    }

    /// <summary>
    /// Creates a retry policy with exponential backoff.
    /// </summary>
    /// <returns>The retry policy.</returns>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                3, // Retry 3 times
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff: 2, 4, 8 seconds
                (result, timeSpan, retryCount, context) =>
                {
                    // Log retry attempts if needed
                    // This can be replaced with actual logging in a real implementation
                    Console.WriteLine($"Request failed with {result.Result?.StatusCode}. Waiting {timeSpan} before retry. Attempt {retryCount}");
                });
    }

    /// <summary>
    /// Creates a circuit breaker policy to prevent overwhelming failing services.
    /// </summary>
    /// <returns>The circuit breaker policy.</returns>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                5, // Number of exceptions before breaking circuit
                TimeSpan.FromMinutes(1)); // Break duration
    }
}
