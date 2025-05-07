using FluentValidation;
using FluentValidation.AspNetCore;
using JobTriggerPlatform.Application.Abstractions;
using JobTriggerPlatform.Domain.Identity;
using JobTriggerPlatform.Infrastructure.Authorization;
using JobTriggerPlatform.Infrastructure.DependencyInjection;
using JobTriggerPlatform.Infrastructure.Persistence;
using JobTriggerPlatform.WebApi;
using JobTriggerPlatform.WebApi.Middleware;
using JobTriggerPlatform.WebApi.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

try
{
    // Ensure plugins directory exists
    var pluginsDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins");
    if (!Directory.Exists(pluginsDirectory))
    {
        Directory.CreateDirectory(pluginsDirectory);
        Console.WriteLine($"Created plugins directory at {pluginsDirectory}");
    }
    
    // Configure standard logging
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();

    // Add services to the container
    builder.Services.AddEndpointsApiExplorer();
    
    // Add OpenAPI support
    builder.Services.AddOpenApiDocument(config => {
        config.Title = "JobTriggerPlatform API";
        config.Description = "API for triggering and managing deployment jobs";
        config.Version = "v1";
    });

    // Configure antiforgery
    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-XSRF-TOKEN";
        options.Cookie.Name = "XSRF-TOKEN";
        options.Cookie.HttpOnly = false; // Allow JavaScript to access the cookie value
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

    // We'll use our custom SecurityHeadersMiddleware instead of the package
    // Configure rate limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Add a named policy for user-based rate limiting
        options.AddPolicy<string>("user-based-policy", context =>
        {
            var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<UserBasedRateLimiterPolicy>();
            var policy = new UserBasedRateLimiterPolicy(
                permitLimit: 100,  // 100 requests
                windowInMinutes: 1,  // per minute
                queueLimit: 0,     // no queuing
                logger);
                
            return policy.GetPartition(context);
        });

        // Add a fixed window limiter for global rate limiting
        options.AddFixedWindowLimiter("Global", options =>
        {
            options.Window = TimeSpan.FromMinutes(1);
            options.PermitLimit = 1000;
            options.QueueLimit = 0;
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });

        // Add a concurrency limiter to limit the number of concurrent requests
        options.AddConcurrencyLimiter("Concurrency", options =>
        {
            options.PermitLimit = 100;
            options.QueueLimit = 50;
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });
    });

    // Add HTTP context accessor
    builder.Services.AddHttpContextAccessor();

    // Add Infrastructure Services (including Identity and Email)
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // Register authorization handlers
    builder.Services.AddScoped<IAuthorizationHandler, JobAccessHandler>();
    builder.Services.AddScoped<IAuthorizationHandler, JobAccessFromRouteHandler>();

    // Configure forwarded headers for proxies
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("192.168.0.0"), 16));
    });

    // Configure cookie policy
    builder.Services.Configure<CookiePolicyOptions>(options =>
    {
        options.MinimumSameSitePolicy = SameSiteMode.Strict;
        options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
        options.Secure = CookieSecurePolicy.Always;
    });

    // Configure Authentication with support for both Identity cookies and JWT
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = true; // Require HTTPS in production
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidAudience = builder.Configuration["JWT:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT:Secret is not configured"))),
            ClockSkew = TimeSpan.Zero // Reduce the default 5 minute clock skew
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var result = JsonSerializer.Serialize(new { message = "You are not authorized to access this resource" });
                return context.Response.WriteAsync(result);
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var result = JsonSerializer.Serialize(new { message = "You do not have permission to access this resource" });
                return context.Response.WriteAsync(result);
            }
        };
    });

    // Set up default authorization policies
    var authBuilder = builder.Services.AddAuthorizationBuilder()
        .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"))
        .AddPolicy("RequireOperatorRole", policy => policy.RequireRole("Operator"))
        .AddPolicy("RequireViewerRole", policy => policy.RequireRole("Viewer"));

    // Create access policies for roles
    authBuilder.AddPolicy("ViewDeploymentJobs", policy => 
        policy.RequireRole("Admin", "Operator", "Viewer"));
        
    authBuilder.AddPolicy("ManageDeploymentJobs", policy => 
        policy.RequireRole("Admin", "Operator"));
        
    authBuilder.AddPolicy("AdminJobs", policy => 
        policy.RequireRole("Admin"));
        
    // Add a fallback policy
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });

    // Add API security options
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.SuppressModelStateInvalidFilter = false;
    });

    // Add Controllers with input validation
    builder.Services.AddControllers(options =>
    {
        // Limit the maximum model binding size
        options.MaxModelBindingCollectionSize = 1000; // Maximum items in a collection
        options.MaxModelBindingRecursionDepth = 8;    // Maximum depth for complex objects

        // Add global Auto-Validate Anti-forgery token filter
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

        // Add global limits for all requests
        options.MaxIAsyncEnumerableBufferLimit = 10 * 1024 * 1024; // 10MB
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Customize model validation responses
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Detail = "See the errors property for details.",
                Instance = context.HttpContext.Request.Path
            };

            return new BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });

    // Add FluentValidation without auto-registration
    builder.Services.AddFluentValidation();
    // Manually register any validators we need except JobTriggerRequestValidator
    // This avoids the circular dependency issue

    // Configure body size limits
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = 1 * 1024 * 1024; // 1MB
        options.Limits.MaxRequestHeadersTotalSize = 32 * 1024; // 32KB
        options.Limits.MaxRequestLineSize = 8 * 1024; // 8KB
        options.Limits.MaxConcurrentConnections = 100; // Limit concurrent connections
        options.Limits.MaxConcurrentUpgradedConnections = 10; // Limit websocket connections
        options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2); // Limit keep-alive connection timeout
        options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30); // Limit time for receiving headers
    });

    // Add CORS with strict policy
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" })
                .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                .WithHeaders("Authorization", "Content-Type", "Accept", "X-XSRF-TOKEN")
                .WithExposedHeaders("X-Pagination", "X-XSRF-TOKEN")
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        });
    });

    var app = builder.Build();

    // Use forwarded headers
    app.UseForwardedHeaders();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        // Configure OpenAPI/Swagger middleware
        app.UseOpenApi();
        app.UseSwaggerUi();
    }
    else
    {
        // Use HSTS in production
        app.UseHsts();
    }

    app.UseStaticFiles();

    // Use cookie policy
    app.UseCookiePolicy();

    // Use input size limit middleware
    app.UseInputSizeLimit(1 * 1024 * 1024); // 1MB

    // Use global exception handler
    app.UseGlobalExceptionHandler();

    // Use security headers (custom middleware)
    app.UseMiddleware<SecurityHeadersMiddleware>();

    // Enable HTTPS redirection
    app.UseHttpsRedirection();

    // Use rate limiting
    app.UseRateLimiter();

    // Use CORS
    app.UseCors();

    // Authentication and authorization
    app.UseAuthentication();

    // Use the user name logging middleware
    app.UseLogUserName();

    // Use antiforgery middleware
    app.UseApiAntiForgery();

    app.UseAuthorization();

    // Map controllers
    var controllerEndpoints = app.MapControllers();

    // Apply rate limiting to controller endpoints
    controllerEndpoints.RequireRateLimiting("Concurrency");

    // Comment out the minimal API job endpoints to avoid route conflicts
    // var jobEndpoints = app.MapJobEndpoints();
    
    // For each job endpoint, apply rate limiting
    // foreach (var endpoint in jobEndpoints)
    // {
    //     endpoint.RequireRateLimiting("Global");
    // }

    // Map root endpoint
    var rootGroup = app.MapGroup("/");
    rootGroup.MapGet("/", () => "JobTriggerPlatform API is running")
        .WithName("GetRoot")
        .WithTags("Root")
        .RequireRateLimiting("Global");

    // Apply database migrations and initialize database
    await DatabaseInitializer.InitializeDatabaseAsync(app.Services);

    // Seed roles and users
    try
    {
        using var scope = app.Services.CreateScope();
        
        // Verify role manager is registered
        if (scope.ServiceProvider.GetService<RoleManager<ApplicationRole>>() == null)
        {
            throw new InvalidOperationException("RoleManager<ApplicationRole> is not registered in the service container.");
        }
        
        await RoleSeeder.SeedRolesAsync(app.Services);
        await UserSeeder.SeedUsersAsync(app.Services);

        // Log the loaded plugins
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var plugins = scope.ServiceProvider.GetRequiredService<IEnumerable<IJobTriggerPlugin>>();

        foreach (var plugin in plugins)
        {
            logger.LogInformation("Loaded plugin: {PluginName} with required roles: {RequiredRoles}",
                plugin.JobName, string.Join(", ", plugin.RequiredRoles));
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while seeding roles or configuring authorization: {ex.Message}");
        Console.WriteLine(ex.StackTrace); // Add stack trace for better debugging
    }

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Application start-up failed: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
