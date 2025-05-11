# PROJECT_LLM_CONTEXT - JobTriggerPlatform

## Project Overview

JobTriggerPlatform is a secure, plugin-based deployment portal for managing automated jobs across multiple environments. It provides a comprehensive solution for managing deployment jobs and automation workflows with a focus on security, extensibility, and usability.

The platform follows a clean architecture pattern with clear separation of concerns between the frontend (React) and backend (.NET Core) components, connected through a RESTful API. Its plugin-based architecture allows for easy extension and customization.

## Backend (.NET Web API - JobTriggerPlatform.WebApi)

### Core Technologies & Version:
- .NET 9 SDK
- ASP.NET Core 9 Web API
- Entity Framework Core (for data access)
- NSwag (for OpenAPI/Swagger documentation)
- PostgreSQL (database)
- Identity with JWT Authentication
- Serilog (for structured logging)
- Seq (for log aggregation)
- FluentValidation (for request validation)

### Project Structure (Key Namespaces/Folders):
- JobTriggerPlatform.Domain: Core entities and business logic
- JobTriggerPlatform.Application: Application services and interfaces
- JobTriggerPlatform.Infrastructure: Infrastructure implementations
- JobTriggerPlatform.WebApi: Controllers, middleware, and startup configuration
- JobTriggerPlatform.Tests: Test project

### API Endpoints:

#### Authentication Endpoints:
- POST `/api/Auth/register`: Register a new user
  - Request: RegisterModel (Email, Password, ConfirmPassword, FullName)
  - Response: UserDto (Id, Email, FullName, Roles)
  - Authorization: None
- POST `/api/Auth/login`: Authenticate a user
  - Request: LoginModel (Email, Password, RememberMe)
  - Response: UserDto with JWT token stored in HttpOnly cookie
  - Authorization: None
- POST `/api/Auth/logout`: Log out a user
  - Request: None
  - Response: 200 OK
  - Authorization: Authenticated user
- GET `/api/Auth/status`: Check authentication status
  - Request: None
  - Response: UserDto
  - Authorization: None
- POST `/api/Auth/refresh-token`: Refresh the JWT token
  - Request: None
  - Response: 200 OK with new token in cookie
  - Authorization: Authenticated user (via expired token)

#### Job Endpoints:
- GET `/api/Jobs`: Get all jobs the user has access to
  - Request: None
  - Response: List of JobViewModel
  - Authorization: Authenticated user with appropriate role/access
- GET `/api/jobs/{jobName}`: Get details for a specific job
  - Request: jobName (string)
  - Response: JobViewModel
  - Authorization: "JobAccess" policy (user must have access to the specific job)
- POST `/api/jobs/{jobName}/trigger`: Trigger a job execution
  - Request: jobName (string), parameters (Dictionary<string, object>)
  - Response: PluginResult
  - Authorization: "JobAccess" policy

#### User Management Endpoints:
- GET `/api/Users`: Get all users
  - Request: None
  - Response: List of UserDto
  - Authorization: "RequireAdminRole" policy
- GET `/api/Users/{id}`: Get a specific user
  - Request: id (string)
  - Response: UserDto
  - Authorization: "RequireAdminRole" policy
- POST `/api/Users`: Create a new user
  - Request: CreateUserModel
  - Response: UserDto
  - Authorization: "RequireAdminRole" policy
- PUT `/api/Users/{id}`: Update a user
  - Request: id (string), UpdateUserModel
  - Response: UserDto
  - Authorization: "RequireAdminRole" policy
- DELETE `/api/Users/{id}`: Delete a user
  - Request: id (string)
  - Response: 204 No Content
  - Authorization: "RequireAdminRole" policy

#### Role Management Endpoints:
- GET `/api/Roles`: Get all roles
  - Request: None
  - Response: List of RoleDto
  - Authorization: "RequireAdminRole" policy
- GET `/api/Roles/{id}`: Get a specific role
  - Request: id (string)
  - Response: RoleDto
  - Authorization: "RequireAdminRole" policy
- POST `/api/Roles`: Create a new role
  - Request: CreateRoleModel
  - Response: RoleDto
  - Authorization: "RequireAdminRole" policy
- PUT `/api/Roles/{id}`: Update a role
  - Request: id (string), UpdateRoleModel
  - Response: RoleDto
  - Authorization: "RequireAdminRole" policy
- DELETE `/api/Roles/{id}`: Delete a role
  - Request: id (string)
  - Response: 204 No Content
  - Authorization: "RequireAdminRole" policy

#### Job Access Endpoints:
- GET `/api/JobAccess/user/{userId}`: Get jobs a user has access to
  - Request: userId (string)
  - Response: List of JobAccessDto
  - Authorization: "RequireAdminRole" policy
- POST `/api/JobAccess/grant`: Grant job access to a user
  - Request: GrantJobAccessModel (UserId, JobName)
  - Response: JobAccessDto
  - Authorization: "RequireAdminRole" policy
- DELETE `/api/JobAccess/revoke`: Revoke job access from a user
  - Request: RevokeJobAccessModel (UserId, JobName)
  - Response: 204 No Content
  - Authorization: "RequireAdminRole" policy

### Authentication & Authorization:

#### Authentication Method:
- JWT (JSON Web Tokens) with HttpOnly cookies
- Token expiry: 60 minutes by default (configurable)
- Automatic token refresh
- Optional 2FA with TOTP

#### Authorization:
- Role-based access control with three default roles:
  - Admin: Full access to all features
  - Operator: Access to view and trigger jobs
  - Viewer: Read-only access to view jobs
- Resource-based permissions for job-specific access
- Custom policies and requirements:
  - "RequireAdminRole": Requires Admin role
  - "RequireOperatorRole": Requires Operator role
  - "JobAccess": Checks if user has access to a specific job

#### Authorization Handlers:
- JobAccessHandler: Enforces job access permissions
- JobAccessFromRouteHandler: Extracts job name from route data

### Plugin System:

#### Interface: IJobTriggerPlugin
```csharp
public interface IJobTriggerPlugin
{
    string JobName { get; }
    string Description { get; }
    string Version { get; }
    IEnumerable<PluginParameter> Parameters { get; }
    IEnumerable<string> RequiredRoles { get; }
    Task<PluginResult> TriggerAsync(PluginParameter[] parameters);
}
```

#### Key Models:

##### PluginParameter
```csharp
public class PluginParameter
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public ParameterType Type { get; set; }
    public bool IsRequired { get; set; }
    public object DefaultValue { get; set; }
    public IEnumerable<object> PossibleValues { get; set; }
    public object Value { get; set; }
}

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
```

##### PluginResult
```csharp
public class PluginResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public object Data { get; set; }
    public object Details { get; set; }
    public IEnumerable<string> Logs { get; set; }
}
```

#### Loading:
- PluginLoader loads plugins from configured directories
- Plugins are registered with PluginRegistry
- Plugins are discovered at startup

#### Example Plugins:
- SampleJobTriggerPlugin: Basic example plugin
- AdvancedJobTriggerPlugin: More complex example with multiple parameters
- SingleTenantPlugin: Plugin with tenant-specific functionality

### Database (ApplicationDbContext.cs):

#### ORM: Entity Framework Core

#### Key Entities:
- ApplicationUser: Custom identity user with additional properties
  - FullName
  - CreatedAt
  - LastLogin
  - IsActive
- ApplicationRole: Custom identity role with additional properties
  - Description
  - CreatedAt
  - IsDefault
- JobAccess: Links users to specific jobs they can access
  - UserId
  - JobName
  - GrantedAt
  - GrantedBy
- JobHistory: Records job execution history
  - JobName
  - UserId
  - ExecutedAt
  - Success
  - Parameters
  - ResultData
- JobLogs: Detailed logs for job executions
  - JobHistoryId
  - Timestamp
  - Level
  - Message

### Configuration:

#### appsettings.json:
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

#### appsettings.Development.json:
- Similar to appsettings.json but with development-specific settings

### Key Services:
- IEmailService: Interface for sending emails
- IJenkinsService: Interface for interacting with Jenkins
- IPluginLoader: Interface for loading plugins
- IPluginRegistry: Interface for managing plugins
- IUserAccessService: Interface for managing user access to jobs

### Middleware:
- SecurityHeadersMiddleware: Adds security headers to responses
- ExceptionHandlingMiddleware: Global exception handler
- ApiAntiForgeryCookieMiddleware: Handles anti-forgery tokens
- LogUserNameMiddleware: Logs the current user name
- InputSizeLimitMiddleware: Limits request size

## Frontend (React - /frontend directory)

### Core Technologies:
- React 18 with TypeScript
- Material UI 7 (for UI components)
- React Router 7 (for routing)
- React Hook Form with Zod validation
- Axios (for API requests)
- MSW (for API mocking during development)
- React-Helmet-Async (for Content Security Policy)
- DOMPurify (for HTML sanitization)

### Project Structure:
```
frontend/
├── public/               # Public assets
├── src/
│   ├── api/              # API client and services
│   ├── auth/             # Authentication system
│   ├── components/       # UI components
│   │   └── shared/       # Shared/reusable components
│   ├── contexts/         # React contexts
│   ├── hooks/            # Custom React hooks
│   ├── mocks/            # MSW mock service workers
│   ├── pages/            # Page components
│   ├── plugins/          # Plugin system and plugins
│   ├── tests/            # Test setup and utils
│   ├── types/            # TypeScript types and interfaces
│   ├── utils/            # Utility functions
│   ├── App.tsx           # Main App component
│   ├── main.tsx          # Entry point
│   ├── theme.tsx         # MUI theme configuration
│   └── vite-env.d.ts     # Vite type declarations
├── vite.config.ts        # Vite configuration
└── vitest.config.ts      # Vitest configuration
```

### Key Features:

#### Authentication System:
- JWT-based authentication with HttpOnly cookies
- Role-based access control
- Resource-based permissions (job-specific access)
- Automatic token refresh
- Protected routes

#### Job Management:
- Listing of available jobs filtered by user's permissions
- Job details view with parameter form generation
- Job triggering with real-time status updates
- Job execution history and logs

#### Admin Features:
- User management (create, view, update, delete)
- Role management (create, view, update, delete)
- Job access management (grant, revoke)

#### Plugin System:
- Frontend plugin registration and management
- Lazy loading support for plugins
- Plugin lifecycle hooks
- Type-safe plugin API
- Notification and Theme plugins included

### API Interaction:
```typescript
// apiClient.ts
import axios from 'axios';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '/api',
  withCredentials: true, // For HttpOnly cookies
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add request interceptor for CSRF token
apiClient.interceptors.request.use(
  (config) => {
    const token = document.cookie
      .split('; ')
      .find((row) => row.startsWith('XSRF-TOKEN='))
      ?.split('=')[1];
    
    if (token) {
      config.headers['X-XSRF-TOKEN'] = token;
    }
    
    return config;
  },
  (error) => Promise.reject(error)
);

// Add response interceptor for 401 responses
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      try {
        await apiClient.post('/Auth/refresh-token');
        return apiClient.request(error.config);
      } catch (refreshError) {
        // Redirect to login page
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }
    return Promise.reject(error);
  }
);

export default apiClient;
```

## Deployment & Operations

### Docker:

#### docker-compose.yml services:
- webapi: JobTriggerPlatform.WebApi container
- frontend: React frontend container
- postgres: PostgreSQL database
- seq: Seq log server

#### Environment Variables:
- ASPNETCORE_ENVIRONMENT: Development/Production
- ConnectionStrings__DefaultConnection: Database connection string
- JWT__Secret: Secret key for JWT tokens
- AllowedOrigins: Comma-separated list of allowed origins for CORS
- API_URL: URL for the API (used by frontend)

### Startup Scripts:
- docker-start.sh (Unix/Linux/Mac)
- docker-start.ps1 (Windows)

### Migrations:
- EF Core migrations are applied automatically on startup
- Manual migration commands are documented in MIGRATIONS.md

### Logging:
- Serilog for structured logging
- Seq for log aggregation and visualization
- Log levels configurable in appsettings.json

## Key Source Files & Documentation Pointers:

### Backend:
- Program.cs: Application entry point and configuration
- JobListController.cs: Controller for listing available jobs
- JobEndpoints.cs: Minimal API endpoints for job operations
- AuthController.cs: Authentication controller
- IJobTriggerPlugin.cs: Plugin interface
- PluginParameter.cs: Plugin parameter model
- PluginResult.cs: Plugin result model
- PluginLoader.cs: Plugin loading implementation
- PluginRegistry.cs: Plugin registry implementation
- JobAccessHandler.cs: Authorization handler for job access
- SecurityHeadersMiddleware.cs: Security headers middleware
- ExceptionHandlingMiddleware.cs: Global exception handler

### Frontend:
- main.tsx: Application entry point
- App.tsx: Main application component
- auth/AuthProvider.tsx: Authentication context provider
- auth/ProtectedRoute.tsx: Route guard component
- plugins/PluginManager.ts: Plugin system implementation
- api/client.ts: API client with interceptors
- pages/Jobs/JobList.tsx: Job listing page
- pages/Jobs/JobDetail.tsx: Job details page
- pages/Admin/Users.tsx: User management page
- pages/Admin/Roles.tsx: Role management page

### Documentation:
- README.md: Main project documentation
- MIGRATIONS.md: Database migration instructions
- frontend/README.md: Frontend-specific documentation
- frontend/src/auth/README.md: Authentication system documentation
- frontend/src/plugins/README.md: Plugin system documentation
- frontend/docs/ContentSecurityPolicy.md: CSP implementation details
- frontend/docs/HTMLSanitization.md: HTML sanitization approach
- terraform/README.md: Terraform deployment documentation
- src/JobTriggerPlatform.Tests/README.md: Testing documentation
