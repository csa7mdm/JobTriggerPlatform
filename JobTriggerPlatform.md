# JobTriggerPlatform - Project Documentation

## Table of Contents
- [Overview](#overview)
- [Project Structure](#project-structure)
- [Backend (.NET Core)](#backend-net-core)
  - [Core Architecture](#core-architecture)
  - [Program.cs Structure](#programcs-structure)
  - [Controllers](#controllers)
  - [Plugin System](#plugin-system)
  - [Authentication and Authorization](#authentication-and-authorization)
  - [Data Models](#data-models)
  - [Database Structure](#database-structure)
  - [API Endpoints](#api-endpoints)
- [Frontend (React)](#frontend-react)
  - [Core Components](#core-components)
  - [Authentication System](#authentication-system)
  - [Plugin System](#frontend-plugin-system)
  - [Security Features](#security-features)
- [Configuration and Settings](#configuration-and-settings)
- [Deployment](#deployment)
  - [Docker Deployment](#docker-deployment)
  - [Terraform Deployment](#terraform-deployment)
- [Database Migrations](#database-migrations)
- [Testing](#testing)
- [Documentation Files](#documentation-files)

## Overview

JobTriggerPlatform is a comprehensive solution for managing deployment jobs and automation workflows. It provides a secure, extensible platform that can be easily customized through its plugin architecture. The platform follows a clean architecture pattern with clear separation of concerns, featuring both a .NET Core backend API and a React frontend.

Key features include:
- Plugin-based architecture for extensibility
- Role-based access control
- Secure authentication with JWT and optional 2FA
- Real-time job monitoring and notifications
- Support for multiple deployment environments
- Containerized deployment with Docker
- Infrastructure as Code with Terraform
- Automatic database migrations
- Test users and authentication tools for development

## Project Structure

The repository is organized as follows:

```
deployment_portal/
├── src/
│   ├── JobTriggerPlatform.Domain/        # Domain entities and business rules
│   ├── JobTriggerPlatform.Application/   # Application services and interfaces
│   ├── JobTriggerPlatform.Infrastructure/# Infrastructure implementations
│   ├── JobTriggerPlatform.WebApi/        # ASP.NET Core Web API
│   └── JobTriggerPlatform.Tests/         # Test project
├── frontend/                             # React frontend application
│   ├── src/
│   │   ├── api/                          # API client and services
│   │   ├── auth/                         # Authentication system
│   │   ├── components/                   # UI components
│   │   ├── pages/                        # Page components
│   │   ├── plugins/                      # Plugin system and plugins
│   │   └── ...
├── terraform/                            # Terraform infrastructure as code
├── docker-compose.yml                    # Docker Compose configuration
├── Dockerfile                            # Backend Dockerfile
├── frontend/Dockerfile                   # Frontend Dockerfile
├── docker-start.sh                       # Unix startup script
├── docker-start.ps1                      # Windows startup script
└── README.md                             # Main project README
```

## Backend (.NET Core)

### Core Architecture

The backend follows a clean architecture pattern with the following layers:

1. **Domain Layer**: Contains entities, enums, value objects, and business logic.
2. **Application Layer**: Contains application services, interfaces, and DTOs.
3. **Infrastructure Layer**: Contains implementations of interfaces defined in the application layer.
4. **WebApi Layer**: Contains controllers, middleware, and startup configuration.

### Program.cs Structure

The `Program.cs` file is the entry point of the application and contains the following key configurations:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options => {
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Authentication
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
    };

    // Configure JWT to use cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["X-Access-Token"];
            return Task.CompletedTask;
        }
    };
});

// Configure Authorization
builder.Services.AddAuthorization(options => {
    // Add policies
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireOperator", policy => policy.RequireRole("Admin", "Operator"));

    // Add job-specific policies
    options.AddPolicy("JobAccess", policy => 
        policy.Requirements.Add(new JobAccessRequirement()));
});

// Register authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, JobAccessHandler>();

// Configure CORS
builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy => {
        policy.WithOrigins(builder.Configuration["AllowedOrigins"].Split(','))
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Register services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJenkinsService, JenkinsService>();

// Configure plugins
builder.Services.AddSingleton<IPluginLoader, PluginLoader>();
builder.Services.AddSingleton<IPluginRegistry>(sp => {
    var pluginLoader = sp.GetRequiredService<IPluginLoader>();
    var pluginRegistry = new PluginRegistry();
    var plugins = pluginLoader.LoadPlugins();
    foreach (var plugin in plugins)
    {
        pluginRegistry.RegisterPlugin(plugin);
    }
    return pluginRegistry;
});

// Configure middleware
builder.Services.AddTransient<SecurityHeadersMiddleware>();
builder.Services.AddTransient<ExceptionHandlingMiddleware>();
builder.Services.AddTransient<ApiAntiForgeryCookieMiddleware>();

// Configure logging
builder.Host.UseSerilog((context, configuration) => {
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"]);
});

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Development-only middleware
    app.MapGet("/api/TestAuth/login/{role}", (string role) => {
        // Development-only test authentication endpoint
    });
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AllowFrontend");

app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<ApiAntiForgeryCookieMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapJobEndpoints();

// Auto-migrate database in development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
        
        // Seed initial data
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        await SeedData.SeedRolesAsync(roleManager);
        await SeedData.SeedUsersAsync(userManager);
    }
}

app.Run();
```

### Controllers

The backend consists of several key controllers:

#### AuthController.cs

Handles user authentication and token management:

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _emailService = emailService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        // Implementation...
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        // Implementation...
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // Implementation...
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetUserStatus()
    {
        // Implementation...
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        // Implementation...
    }

    private async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        // Implementation...
    }

    private void SetTokenCookie(string token)
    {
        // Implementation...
    }
}
```

#### JobsController.cs (Refactored to JobListController.cs)

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobListController : ControllerBase
{
    private readonly IPluginRegistry _pluginRegistry;

    public JobListController(IPluginRegistry pluginRegistry)
    {
        _pluginRegistry = pluginRegistry;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs()
    {
        // Get all registered plugins and return as job list
        var plugins = _pluginRegistry.GetAllPlugins();

        // Filter plugins based on user's roles and permissions
        var user = await _userManager.GetUserAsync(User);
        var userRoles = await _userManager.GetRolesAsync(user);
        
        // Filter jobs the user has access to
        var accessibleJobs = plugins
            .Where(p => userRoles.Intersect(p.RequiredRoles).Any() || 
                   await _userAccessService.HasJobAccessAsync(user.Id, p.Name))
            .Select(p => new JobViewModel
            {
                Name = p.Name,
                Description = p.Description,
                Parameters = p.Parameters.Select(param => new ParameterViewModel
                {
                    Name = param.Name,
                    DisplayName = param.DisplayName,
                    Description = param.Description,
                    Type = param.Type.ToString(),
                    IsRequired = param.IsRequired,
                    DefaultValue = param.DefaultValue,
                    PossibleValues = param.PossibleValues
                }).ToList()
            });

        return Ok(accessibleJobs);
    }
}
```

#### JobEndpoints.cs

Minimal API endpoints for job operations:

```csharp
public static class JobEndpoints
{
    public static void MapJobEndpoints(this WebApplication app)
    {
        // Get job details by name
        app.MapGet("/api/jobs/{jobName}", async (string jobName, IPluginRegistry pluginRegistry) =>
        {
            var plugin = pluginRegistry.GetPlugin(jobName);
            if (plugin == null)
                return Results.NotFound();

            return Results.Ok(new JobViewModel
            {
                Name = plugin.Name,
                Description = plugin.Description,
                Parameters = plugin.Parameters.Select(p => new ParameterViewModel
                {
                    Name = p.Name,
                    DisplayName = p.DisplayName,
                    Description = p.Description,
                    Type = p.Type.ToString(),
                    IsRequired = p.IsRequired,
                    DefaultValue = p.DefaultValue,
                    PossibleValues = p.PossibleValues
                }).ToList()
            });
        })
        .RequireAuthorization("JobAccess");

        // Trigger a job
        app.MapPost("/api/jobs/{jobName}/trigger", async (string jobName, 
            [FromBody] Dictionary<string, object> parameters,
            IPluginRegistry pluginRegistry) =>
        {
            var plugin = pluginRegistry.GetPlugin(jobName);
            if (plugin == null)
                return Results.NotFound();

            // Validate parameters
            var pluginParameters = new List<PluginParameter>();
            foreach (var param in plugin.Parameters)
            {
                if (parameters.TryGetValue(param.Name, out var value))
                {
                    // Convert value to proper type
                    pluginParameters.Add(new PluginParameter
                    {
                        Name = param.Name,
                        Value = ConvertValue(value, param.Type)
                    });
                }
                else if (param.IsRequired)
                {
                    return Results.BadRequest($"Required parameter '{param.Name}' is missing");
                }
            }

            try
            {
                // Execute the plugin
                var result = await plugin.ExecuteAsync(pluginParameters.ToArray());
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        })
        .RequireAuthorization("JobAccess");
    }

    private static object ConvertValue(object value, ParameterType type)
    {
        // Implementation...
    }
}
```

### Plugin System

The backend plugin system is based on the following key components:

#### IJobTriggerPlugin.cs

```csharp
namespace JobTriggerPlatform.Application.Abstractions
{
    public interface IJobTriggerPlugin
    {
        /// <summary>
        /// The name of the job plugin
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Description of what the job does
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Plugin version
        /// </summary>
        string Version { get; }
        
        /// <summary>
        /// The parameters required by this job
        /// </summary>
        IEnumerable<PluginParameter> Parameters { get; }
        
        /// <summary>
        /// Roles that are allowed to access this job
        /// </summary>
        IEnumerable<string> RequiredRoles { get; }
        
        /// <summary>
        /// Execute the job with the given parameters
        /// </summary>
        /// <param name="parameters">Parameters for the job</param>
        /// <returns>A result object containing information about the job execution</returns>
        Task<PluginResult> ExecuteAsync(PluginParameter[] parameters);
    }
}
```

#### PluginParameter.cs

```csharp
namespace JobTriggerPlatform.Application.Abstractions
{
    /// <summary>
    /// Parameter type enumeration
    /// </summary>
    public enum ParameterType
    {
        String,
        Number,
        Boolean,
        DateTime,
        Select,
        MultiSelect,
        File
    }

    /// <summary>
    /// Represents a parameter for a job plugin
    /// </summary>
    public class PluginParameter
    {
        /// <summary>
        /// The name of the parameter
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Display name for the parameter
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Description of the parameter
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// The type of the parameter
        /// </summary>
        public ParameterType Type { get; set; }
        
        /// <summary>
        /// Whether the parameter is required
        /// </summary>
        public bool IsRequired { get; set; }
        
        /// <summary>
        /// Default value for the parameter
        /// </summary>
        public object DefaultValue { get; set; }
        
        /// <summary>
        /// Possible values for Select and MultiSelect types
        /// </summary>
        public IEnumerable<object> PossibleValues { get; set; }
        
        /// <summary>
        /// The actual value of the parameter
        /// </summary>
        public object Value { get; set; }
    }
}
```

#### PluginResult.cs

```csharp
namespace JobTriggerPlatform.Application.Abstractions
{
    /// <summary>
    /// Represents the result of a job execution
    /// </summary>
    public class PluginResult
    {
        /// <summary>
        /// Whether the job was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// A message describing the result
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Data returned by the job
        /// </summary>
        public object Data { get; set; }
        
        /// <summary>
        /// Additional details about the execution
        /// </summary>
        public object Details { get; set; }
        
        /// <summary>
        /// Logs from the job execution
        /// </summary>
        public IEnumerable<string> Logs { get; set; }
    }
}
```

#### PluginLoader.cs

```csharp
namespace JobTriggerPlatform.Infrastructure.Plugins
{
    /// <summary>
    /// Loads plugins from assemblies
    /// </summary>
    public class PluginLoader : IPluginLoader
    {
        private readonly ILogger<PluginLoader> _logger;
        private readonly IConfiguration _configuration;
        
        public PluginLoader(ILogger<PluginLoader> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        
        /// <summary>
        /// Loads plugins from the configured plugin directories
        /// </summary>
        public IEnumerable<IJobTriggerPlugin> LoadPlugins()
        {
            var plugins = new List<IJobTriggerPlugin>();
            var pluginDirs = _configuration.GetSection("Plugins:Directories").Get<string[]>();
            
            foreach (var dir in pluginDirs)
            {
                if (!Directory.Exists(dir))
                {
                    _logger.LogWarning("Plugin directory {Directory} does not exist", dir);
                    continue;
                }
                
                var pluginFiles = Directory.GetFiles(dir, "*.dll");
                foreach (var file in pluginFiles)
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(file);
                        var pluginTypes = assembly.GetExportedTypes()
                            .Where(t => typeof(IJobTriggerPlugin).IsAssignableFrom(t) && !t.IsAbstract);
                            
                        foreach (var pluginType in pluginTypes)
                        {
                            var plugin = (IJobTriggerPlugin)Activator.CreateInstance(pluginType);
                            plugins.Add(plugin);
                            _logger.LogInformation("Loaded plugin {PluginName} from {AssemblyName}", 
                                plugin.Name, assembly.GetName().Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load plugins from {FileName}", file);
                    }
                }
            }
            
            return plugins;
        }
    }
}
```

#### PluginRegistry.cs

```csharp
namespace JobTriggerPlatform.Infrastructure.Plugins
{
    /// <summary>
    /// Registry for managing loaded plugins
    /// </summary>
    public class PluginRegistry : IPluginRegistry
    {
        private readonly Dictionary<string, IJobTriggerPlugin> _plugins = new();
        private readonly ILogger<PluginRegistry> _logger;
        
        public PluginRegistry(ILogger<PluginRegistry> logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Register a plugin with the system
        /// </summary>
        public void RegisterPlugin(IJobTriggerPlugin plugin)
        {
            if (_plugins.ContainsKey(plugin.Name))
            {
                _logger.LogWarning("Plugin with name {PluginName} is already registered", plugin.Name);
                return;
            }
            
            _plugins.Add(plugin.Name, plugin);
            _logger.LogInformation("Registered plugin {PluginName}", plugin.Name);
        }
        
        /// <summary>
        /// Get a plugin by name
        /// </summary>
        public IJobTriggerPlugin GetPlugin(string name)
        {
            if (_plugins.TryGetValue(name, out var plugin))
            {
                return plugin;
            }
            
            return null;
        }
        
        /// <summary>
        /// Get all registered plugins
        /// </summary>
        public IEnumerable<IJobTriggerPlugin> GetAllPlugins()
        {
            return _plugins.Values;
        }
    }
}
```

### Authentication and Authorization

#### Authentication System

The application uses ASP.NET Core Identity with JWT tokens for authentication. Key components include:

1. **HttpOnly Cookie-based JWT Storage** - JWTs are stored in HttpOnly cookies for enhanced security.
2. **Identity Configuration** - Uses `ApplicationUser` and `ApplicationRole` classes for user and role management.
3. **2FA Support** - Optional 2FA with TOTP (Time-based One-Time Password).
4. **Token Refresh** - Automatic token refreshing to maintain sessions.

#### Authorization System

Authorization is implemented using:

1. **Role-Based Policies** - Defined policies like `RequireAdmin` and `RequireOperator`.
2. **Custom Requirements** - `JobAccessRequirement` for job-specific access control.
3. **Authorization Handlers** - `JobAccessHandler` to enforce job access permissions.

#### JobAccessRequirement.cs

```csharp
namespace JobTriggerPlatform.WebApi.Authorization
{
    public class JobAccessRequirement : IAuthorizationRequirement
    {
        // This is a marker class
    }
}
```

#### JobAccessHandler.cs

```csharp
namespace JobTriggerPlatform.WebApi.Authorization
{
    public class JobAccessHandler : AuthorizationHandler<JobAccessRequirement>
    {
        private readonly IUserAccessService _userAccessService;
        private readonly IPluginRegistry _pluginRegistry;
        private readonly UserManager<ApplicationUser> _userManager;
        
        public JobAccessHandler(
            IUserAccessService userAccessService,
            IPluginRegistry pluginRegistry,
            UserManager<ApplicationUser> userManager)
        {
            _userAccessService = userAccessService;
            _pluginRegistry = pluginRegistry;
            _userManager = userManager;
        }
        
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            JobAccessRequirement requirement)
        {
            // Get the jobName from the route data
            var routeData = context.Resource as RouteEndpointContext;
            var jobName = routeData?.RouteData?.Values["jobName"]?.ToString();
            
            if (string.IsNullOrEmpty(jobName))
            {
                // Not a job-specific route, so we succeed
                context.Succeed(requirement);
                return;
            }
            
            // Get the plugin
            var plugin = _pluginRegistry.GetPlugin(jobName);
            if (plugin == null)
            {
                // Plugin not found, we cannot authorize
                return;
            }
            
            // Get the user and their roles
            var user = await _userManager.GetUserAsync(context.User);
            if (user == null)
            {
                return;
            }
            
            var userRoles = await _userManager.GetRolesAsync(user);
            
            // Check if the user has the required roles for the plugin
            if (plugin.RequiredRoles.Any() && plugin.RequiredRoles.Intersect(userRoles).Any())
            {
                context.Succeed(requirement);
                return;
            }
            
            // Check if the user has specific access to this job
            if (await _userAccessService.HasJobAccessAsync(user.Id, jobName))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}
```

### Data Models

#### ApplicationUser.cs

```csharp
namespace JobTriggerPlatform.Domain.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<ApplicationUserRole> UserRoles { get; set; }
        public virtual ICollection<JobAccess> JobAccesses { get; set; }
    }
}
```

#### ApplicationRole.cs

```csharp
namespace JobTriggerPlatform.Domain.Identity
{
    public class ApplicationRole : IdentityRole
    {
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDefault { get; set; }
        
        // Navigation properties
        public virtual ICollection<ApplicationUserRole> UserRoles { get; set; }
    }
}
```

#### JobAccess.cs

```csharp
namespace JobTriggerPlatform.Domain.Entities
{
    public class JobAccess
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string JobName { get; set; }
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
        public string GrantedBy { get; set; }
        
        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }
}
```

### Database Structure

The application uses Entity Framework Core with PostgreSQL. Key tables include:

1. **AspNetUsers** - Stores user data (ApplicationUser)
2. **AspNetRoles** - Stores role data (ApplicationRole)
3. **AspNetUserRoles** - Joins users and roles
4. **JobAccess** - Stores job access permissions
5. **JobHistory** - Stores job execution history
6. **JobLogs** - Stores detailed logs for job executions

### API Endpoints

Key API endpoints include:

#### Authentication Endpoints
- POST `/api/Auth/register` - Register a new user
- POST `/api/Auth/login` - Authenticate a user
- POST `/api/Auth/logout` - Log out a user
- GET `/api/Auth/status` - Check authentication status
- POST `/api/Auth/refresh-token` - Refresh the JWT token

#### Job Endpoints
- GET `/api/Jobs` - Get all jobs the user has access to
- GET `/api/jobs/{jobName}` - Get details for a specific job
- POST `/api/jobs/{jobName}/trigger` - Trigger a job execution

#### User Management Endpoints
- GET `/api/Users` - Get all users (admin only)
- GET `/api/Users/{id}` - Get a specific user (admin only)
- POST `/api/Users` - Create a new user (admin only)
- PUT `/api/Users/{id}` - Update a user (admin only)
- DELETE `/api/Users/{id}` - Delete a user (admin only)

#### Role Management Endpoints
- GET `/api/Roles` - Get all roles (admin only)
- GET `/api/Roles/{id}` - Get a specific role (admin only)
- POST `/api/Roles` - Create a new role (admin only)
- PUT `/api/Roles/{id}` - Update a role (admin only)
- DELETE `/api/Roles/{id}` - Delete a role (admin only)

#### Job Access Endpoints
- GET `/api/JobAccess/user/{userId}` - Get jobs a user has access to
- POST `/api/JobAccess/grant` - Grant job access to a user
- DELETE `/api/JobAccess/revoke` - Revoke job access from a user

## Frontend (React)

### Core Components

The frontend is a React application with TypeScript, using Material UI for UI components. Key directories include:

1. **api/** - API client and services
2. **auth/** - Authentication system
3. **components/** - UI components
4. **contexts/** - React contexts
5. **hooks/** - Custom React hooks
6. **pages/** - Page components
7. **plugins/** - Plugin system and plugins
8. **utils/** - Utility functions

### Authentication System

The frontend authentication system features:

1. **JWT with HttpOnly Cookies** - Secure token storage
2. **Role-Based Access Control** - Components that respect user roles
3. **Automatic Token Refresh** - Background token refresh every 10 minutes
4. **Protected Routes** - Route guards based on authentication and roles

Key components:

#### AuthProvider.tsx

```tsx
import React, { createContext, useContext, useState, useEffect } from 'react';
import { apiClient } from '../api/client';

// Auth context and provider implementation
export const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // Check authentication status
  const checkAuthStatus = async () => {
    try {
      const response = await apiClient.get('/api/Auth/status');
      setUser(response.data);
      setIsAuthenticated(true);
    } catch (error) {
      setUser(null);
      setIsAuthenticated(false);
    } finally {
      setIsLoading(false);
    }
  };

  // Login function
  const login = async (credentials: LoginCredentials) => {
    const response = await apiClient.post('/api/Auth/login', credentials);
    setUser(response.data);
    setIsAuthenticated(true);
    return response.data;
  };

  // Logout function
  const logout = async () => {
    await apiClient.post('/api/Auth/logout');
    setUser(null);
    setIsAuthenticated(false);
  };

  // Check if user has a specific role
  const hasRole = (roles: string | string[]) => {
    if (!user) return false;
    
    if (typeof roles === 'string') {
      return user.roles.includes(roles);
    }
    
    return roles.some(role => user.roles.includes(role));
  };

  // Check if user has access to a specific job
  const canAccessJob = (jobId: string) => {
    if (!user) return false;
    return user.accessibleJobs.includes(jobId);
  };

  // Setup token refresh
  useEffect(() => {
    checkAuthStatus();
    
    // Setup token refresh interval
    const refreshInterval = setInterval(async () => {
      if (isAuthenticated) {
        try {
          await apiClient.post('/api/Auth/refresh-token');
        } catch (error) {
          console.error('Failed to refresh token:', error);
        }
      }
    }, 10 * 60 * 1000); // Refresh every 10 minutes
    
    return () => clearInterval(refreshInterval);
  }, [isAuthenticated]);

  // Setup axios interceptor for 401 responses
  useEffect(() => {
    const interceptor = apiClient.interceptors.response.use(
      response => response,
      async error => {
        if (error.response?.status === 401 && isAuthenticated) {
          try {
            await apiClient.post('/api/Auth/refresh-token');
            return apiClient.request(error.config);
          } catch (refreshError) {
            setUser(null);
            setIsAuthenticated(false);
            return Promise.reject(refreshError);
          }
        }
        return Promise.reject(error);
      }
    );
    
    return () => apiClient.interceptors.response.eject(interceptor);
  }, [isAuthenticated]);

  const value = {
    user,
    isAuthenticated,
    isLoading,
    login,
    logout,
    hasRole,
    canAccessJob
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};

// Custom hook to use the auth context
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
```

#### ProtectedRoute.tsx

```tsx
import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from './AuthProvider';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRoles?: string | string[];
  requiredJobId?: string;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  requiredRoles,
  requiredJobId
}) => {
  const { isAuthenticated, isLoading, hasRole, canAccessJob } = useAuth();
  const location = useLocation();

  if (isLoading) {
    // Show loading indicator
    return <div>Loading...</div>;
  }

  if (!isAuthenticated) {
    // Redirect to login page with return URL
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // Check role-based access if required
  if (requiredRoles && !hasRole(requiredRoles)) {
    // Redirect to unauthorized page
    return <Navigate to="/unauthorized" replace />;
  }

  // Check job-specific access if required
  if (requiredJobId && !canAccessJob(requiredJobId)) {
    // Redirect to unauthorized page
    return <Navigate to="/unauthorized" replace />;
  }

  // User is authenticated and authorized, render the protected content
  return <>{children}</>;
};
```

### Frontend Plugin System

The frontend includes a flexible plugin system for extending functionality:

#### Plugin.ts

```typescript
export interface Plugin {
  id: string;
  name: string;
  version: string;
  description?: string;
  initialize: (options?: any) => void | (() => void);
  onLoad?: () => void;
  onUnload?: () => void;
}

export interface PluginOptions {
  autoInitialize?: boolean;
  config?: Record<string, any>;
}

export interface RegisteredPlugin<T extends Plugin = Plugin> {
  plugin: T;
  options: PluginOptions;
  initialized: boolean;
}
```

#### PluginManager.ts

```typescript
export class PluginManager {
  private plugins: Map<string, RegisteredPlugin> = new Map();
  
  registerPlugin<T extends Plugin>(
    plugin: T,
    options: PluginOptions = { autoInitialize: true }
  ): void {
    if (this.plugins.has(plugin.id)) {
      console.warn(`Plugin with ID "${plugin.id}" is already registered`);
      return;
    }
    
    const registeredPlugin: RegisteredPlugin<T> = {
      plugin,
      options,
      initialized: false
    };
    
    this.plugins.set(plugin.id, registeredPlugin);
    
    // Call onLoad if available
    if (plugin.onLoad) {
      plugin.onLoad();
    }
    
    // Auto-initialize if enabled
    if (options.autoInitialize) {
      this.initializePlugin(plugin.id);
    }
  }
  
  getPlugin<T extends Plugin>(id: string): T | null {
    const registeredPlugin = this.plugins.get(id);
    return registeredPlugin ? registeredPlugin.plugin as T : null;
  }
  
  getAllPlugins(): Plugin[] {
    return Array.from(this.plugins.values()).map(rp => rp.plugin);
  }
  
  initializePlugin(id: string): void {
    const registeredPlugin = this.plugins.get(id);
    if (!registeredPlugin || registeredPlugin.initialized) {
      return;
    }
    
    try {
      const cleanup = registeredPlugin.plugin.initialize(registeredPlugin.options.config);
      registeredPlugin.initialized = true;
      
      // Store cleanup function if returned
      if (typeof cleanup === 'function') {
        registeredPlugin.cleanup = cleanup;
      }
    } catch (error) {
      console.error(`Failed to initialize plugin "${id}":`, error);
    }
  }
  
  unloadPlugin(id: string): void {
    const registeredPlugin = this.plugins.get(id);
    if (!registeredPlugin) {
      return;
    }
    
    try {
      // Call cleanup function if available
      if (registeredPlugin.cleanup) {
        registeredPlugin.cleanup();
      }
      
      // Call onUnload if available
      if (registeredPlugin.plugin.onUnload) {
        registeredPlugin.plugin.onUnload();
      }
      
      this.plugins.delete(id);
    } catch (error) {
      console.error(`Failed to unload plugin "${id}":`, error);
    }
  }
}

export const pluginManager = new PluginManager();
```

### Security Features

The frontend implements several security features:

1. **Content Security Policy (CSP)** - Implemented with react-helmet-async
2. **HTML Sanitization** - DOMPurify for sanitizing HTML content
3. **CSRF Protection** - Anti-forgery tokens for sensitive operations
4. **Secure Cookie Handling** - HttpOnly, SameSite, and Secure cookies

## Configuration and Settings

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=JobTriggerPlatform;Username=postgres;Password=password"
  },
  "JWT": {
    "Secret": "YOUR_STRONG_SECRET_KEY_HERE",
    "Issuer": "JobTriggerPlatform",
    "Audience": "JobTriggerPlatformApp",
    "ExpiryInMinutes": 60
  },
  "AllowedOrigins": "https://localhost:3000,http://localhost:3000",
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "youremail@gmail.com",
    "SmtpPassword": "yourpassword",
    "SenderEmail": "youremail@gmail.com",
    "SenderName": "Job Trigger Platform"
  },
  "Plugins": {
    "Directories": [
      "./plugins"
    ]
  },
  "Jenkins": {
    "BaseUrl": "https://jenkins.example.com",
    "Username": "jenkinsuser",
    "ApiToken": "your-api-token"
  },
  "Seq": {
    "ServerUrl": "http://seq:5341"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

## Deployment

### Docker Deployment

The application can be deployed using Docker Compose:

#### docker-compose.yml

```yaml
version: '3.8'

services:
  webapi:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:80"
      - "8443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=JobTriggerPlatform;Username=postgres;Password=postgres_password
      - JWT__Secret=${JWT_SECRET}
      - Seq__ServerUrl=http://seq:5341
    volumes:
      - ./plugins:/app/plugins
    depends_on:
      - postgres
      - seq

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    ports:
      - "80:80"
      - "443:443"
    environment:
      - API_URL=http://webapi
    depends_on:
      - webapi

  postgres:
    image: postgres:15
    environment:
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=postgres_password
      - POSTGRES_DB=JobTriggerPlatform
    volumes:
      - postgres-data:/var/lib/postgresql/data
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql
    ports:
      - "5432:5432"

  seq:
    image: datalust/seq:latest
    environment:
      - ACCEPT_EULA=Y
    ports:
      - "5341:80"
    volumes:
      - seq-data:/data

volumes:
  postgres-data:
  seq-data:
```

#### Dockerfile (Backend)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/JobTriggerPlatform.WebApi/JobTriggerPlatform.WebApi.csproj", "src/JobTriggerPlatform.WebApi/"]
COPY ["src/JobTriggerPlatform.Application/JobTriggerPlatform.Application.csproj", "src/JobTriggerPlatform.Application/"]
COPY ["src/JobTriggerPlatform.Domain/JobTriggerPlatform.Domain.csproj", "src/JobTriggerPlatform.Domain/"]
COPY ["src/JobTriggerPlatform.Infrastructure/JobTriggerPlatform.Infrastructure.csproj", "src/JobTriggerPlatform.Infrastructure/"]
RUN dotnet restore "src/JobTriggerPlatform.WebApi/JobTriggerPlatform.WebApi.csproj"
COPY . .
WORKDIR "/src/src/JobTriggerPlatform.WebApi"
RUN dotnet build "JobTriggerPlatform.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JobTriggerPlatform.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobTriggerPlatform.WebApi.dll"]
```

#### Dockerfile (Frontend)

```dockerfile
# Build stage
FROM node:18-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

# Production stage
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

### Terraform Deployment

The application can be deployed to Google Cloud Platform using Terraform:

#### terraform/main.tf

```hcl
variable "project_id" {
  description = "The Google Cloud project ID"
  type        = string
}

variable "region" {
  description = "The region to deploy to"
  type        = string
  default     = "us-central1"
}

variable "environment" {
  description = "The environment to deploy to (dev, prod)"
  type        = string
  default     = "dev"
}

variable "db_password" {
  description = "PostgreSQL database password"
  type        = string
  sensitive   = true
}

variable "jwt_secret" {
  description = "JWT secret key"
  type        = string
  sensitive   = true
}

provider "google" {
  project = var.project_id
  region  = var.region
}

# Cloud SQL PostgreSQL instance
module "cloud_sql" {
  source          = "./modules/cloud_sql"
  project_id      = var.project_id
  region          = var.region
  environment     = var.environment
  db_name         = "job-trigger-platform"
  db_user         = "postgres"
  db_password     = var.db_password
}

# Secret Manager for storing secrets
module "secret_manager" {
  source       = "./modules/secret_manager"
  project_id   = var.project_id
  environment  = var.environment
  jwt_secret   = var.jwt_secret
  db_password  = var.db_password
}

# Cloud Run service for backend API
module "backend_cloud_run" {
  source        = "./modules/cloud_run"
  project_id    = var.project_id
  region        = var.region
  environment   = var.environment
  service_name  = "job-trigger-platform-api"
  image         = "gcr.io/${var.project_id}/job-trigger-platform-api:latest"
  
  # Environment variables from secrets
  secrets = {
    "JWT__Secret"                        = module.secret_manager.jwt_secret_version_name
    "ConnectionStrings__DefaultConnection" = module.secret_manager.db_connection_version_name
  }
  
  # Public service with IAM
  allow_public_access = true
  min_instances       = 1
  max_instances       = 5
}

# Cloud Run service for frontend
module "frontend_cloud_run" {
  source        = "./modules/cloud_run"
  project_id    = var.project_id
  region        = var.region
  environment   = var.environment
  service_name  = "job-trigger-platform-frontend"
  image         = "gcr.io/${var.project_id}/job-trigger-platform-frontend:latest"
  
  # Environment variables
  env_vars = {
    "API_URL" = module.backend_cloud_run.service_url
  }
  
  # Public service with IAM
  allow_public_access = true
  min_instances       = 1
  max_instances       = 5
}

# Cloud Armor WAF policy
module "cloud_armor" {
  source        = "./modules/cloud_armor"
  project_id    = var.project_id
  policy_name   = "job-trigger-platform-waf"
  environment   = var.environment
  
  # IP allow/deny rules
  allowed_ip_ranges = var.environment == "dev" ? ["0.0.0.0/0"] : ["your-company-ip-range"]
  blocked_countries = ["CN", "RU", "IR"]
  
  # WAF rules
  enable_xss_protection     = true
  enable_sqli_protection    = true
  enable_rce_protection     = true
  enable_lfi_protection     = true
  enable_scanner_protection = true
  enable_protocol_attack_protection = true
}

# IAM permissions
module "iam" {
  source     = "./modules/iam"
  project_id = var.project_id
  service_account_name = "job-trigger-platform-sa"
  environment = var.environment
}

# Outputs
output "backend_url" {
  value = module.backend_cloud_run.service_url
}

output "frontend_url" {
  value = module.frontend_cloud_run.service_url
}

output "database_instance_name" {
  value = module.cloud_sql.instance_name
}
```

## Database Migrations

The application uses Entity Framework Core migrations for database schema management:

### Creating a Migration

```bash
dotnet ef migrations add [MigrationName] --project ../JobTriggerPlatform.Infrastructure --startup-project .
```

### Applying Migrations Manually

```bash
dotnet ef database update --project ../JobTriggerPlatform.Infrastructure --startup-project .
```

### Removing the Last Migration

```bash
dotnet ef migrations remove --project ../JobTriggerPlatform.Infrastructure --startup-project .
```

### Generating a SQL Script

```bash
dotnet ef migrations script --project ../JobTriggerPlatform.Infrastructure --startup-project .
```

## Testing

The application includes a test project with comprehensive test coverage:

### Test Structure

The tests are organized to mirror the application's structure:

- **Domain Tests**: Tests for domain entities
- **Application Tests**: Tests for application layer components
- **Infrastructure Tests**: Tests for infrastructure components
- **WebApi Tests**: Tests for API controllers and endpoints

### Running Tests

```bash
dotnet test src/JobTriggerPlatform.Tests/JobTriggerPlatform.Tests.csproj
```

### Generating Code Coverage Report

```bash
dotnet test src/JobTriggerPlatform.Tests/JobTriggerPlatform.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

## Documentation Files

The project includes the following documentation files:

1. **README.md** - Main project documentation
2. **MIGRATIONS.md** - Database migration instructions
3. **frontend/README.md** - Frontend-specific documentation
4. **frontend/src/auth/README.md** - Authentication system documentation
5. **frontend/src/plugins/README.md** - Plugin system documentation
6. **frontend/docs/ContentSecurityPolicy.md** - CSP implementation details
7. **frontend/docs/HTMLSanitization.md** - HTML sanitization approach
8. **terraform/README.md** - Terraform deployment documentation
9. **src/JobTriggerPlatform.Tests/README.md** - Testing documentation
