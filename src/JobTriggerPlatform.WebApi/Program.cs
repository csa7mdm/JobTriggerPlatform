using JobTriggerPlatform.Application.Abstractions;
using JobTriggerPlatform.Domain.Identity;
using JobTriggerPlatform.Infrastructure.Authorization;
using JobTriggerPlatform.Infrastructure.DependencyInjection;
using JobTriggerPlatform.Infrastructure.Persistence;
using JobTriggerPlatform.WebApi;
using JobTriggerPlatform.WebApi.Logging;
using JobTriggerPlatform.WebApi.Middleware;
using JobTriggerPlatform.WebApi.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using NetEscapades.AspNetCore.SecurityHeaders;
using NetEscapades.AspNetCore.SecurityHeaders.Infrastructure;
using Serilog;
using Serilog.Events;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .Enrich.With(new RequestUserIdEnricher(services.GetRequiredService<IHttpContextAccessor>()))
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.Async(a => a.File(
        path: "logs/log-.json",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: LogEventLevel.Information,
        formatter: new Serilog.Formatting.Json.JsonFormatter()))
);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure antiforgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.HttpOnly = false; // Allow JavaScript to access the cookie value
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Configure security headers
builder.Services.AddSecurityHeaders(headers =>
{
    headers.AddDefaultSecurityHeaders()
        .AddStrictTransportSecurityMaxAgeIncludeSubDomains(maxAgeInSeconds: 60 * 60 * 24 * 365) // 1 year
        .AddContentSecurityPolicy(csp =>
        {
            csp.AddDefaultSrc().Self();
            csp.AddScriptSrc().Self().UnsafeInline().UnsafeEval();
            csp.AddStyleSrc().Self().UnsafeInline();
            csp.AddImgSrc().Self().Data();
            csp.AddFontSrc().Self();
            csp.AddConnectSrc().Self();
            csp.AddFrameAncestors().None();
            csp.AddFormAction().Self();
            csp.AddBaseUri().Self();
            csp.AddObjectSrc().None();
            csp.AddUpgradeInsecureRequests();
        })
        .AddPermissionsPolicy(permissions =>
        {
            permissions.AddAccelerometer().None();
            permissions.AddCamera().None();
            permissions.AddGeolocation().None();
            permissions.AddGyroscope().None();
            permissions.AddMagnetometer().None();
            permissions.AddMicrophone().None();
            permissions.AddPayment().None();
            permissions.AddUsb().None();
        })
        .AddReferrerPolicy(ReferrerPolicy.StrictOriginWhenCrossOrigin)
        .AddCrossOriginEmbedderPolicy(CrossOriginEmbedderPolicyValue.RequireCorp)
        .AddCrossOriginOpenerPolicy(CrossOriginOpenerPolicyValue.SameOrigin)
        .AddCrossOriginResourcePolicy(CrossOriginResourcePolicyValue.SameOrigin);
});

// Configure rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Add a named policy for user-based rate limiting
    options.AddPolicy("UserBasedRateLimiter", context =>
    {
        var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<UserBasedRateLimiterPolicy>();
        return new UserBasedRateLimiterPolicy(
            permitLimit: 100,  // 100 requests
            windowInMinutes: 1,  // per minute
            queueLimit: 0,     // no queuing
            logger);
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
    options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
    options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
    options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("192.168.0.0"), 16));
});

// Configure cookie policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
});

// Configure JWT Authentication
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

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    // Basic role-based policies
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireDevRole", policy => policy.RequireRole("Dev"));
    options.AddPolicy("RequireQARole", policy => policy.RequireRole("QA"));
    
    // Initially register placeholder policies for job access
    // These will be properly configured after plugins are loaded
    options.AddPolicy("JobAccess:SampleJob", policy => 
        policy.Requirements.Add(new JobAccessRequirement("SampleJob")));
    
    options.AddPolicy("JobAccess:AdvancedDeployment", policy => 
        policy.Requirements.Add(new JobAccessRequirement("AdvancedDeployment")));

    // Add a fallback policy
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

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();

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
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Use HSTS in production
    app.UseHsts();
}

// Add Serilog request logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
    };
});

// Use cookie policy
app.UseCookiePolicy();

// Use input size limit middleware
app.UseInputSizeLimit(1 * 1024 * 1024); // 1MB

// Use global exception handler
app.UseGlobalExceptionHandler();

// Use security headers
app.UseSecurityHeaders();

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
app.MapControllers()
    .RequireRateLimiting("Concurrency"); // Apply concurrency limit to all controllers

// Apply rate limiting to the job endpoints
app.MapJobEndpoints().RequireRateLimiting("UserBasedRateLimiter");

// Seed roles
try
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    await RoleSeeder.SeedRolesAsync(app.Services);
    
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
    Log.Fatal(ex, "An error occurred while seeding roles or configuring authorization");
}

app.MapGet("/", () => "JobTriggerPlatform API is running")
    .WithName("GetRoot")
    .WithOpenApi()
    .RequireRateLimiting("Global");

// Ensure any buffered events are sent at shutdown
app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

app.Run();
