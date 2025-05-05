# JobTriggerPlatform

A secure, plugin-based deployment portal for managing automated jobs across multiple environments.

![JobTriggerPlatform](https://via.placeholder.com/800x400?text=JobTriggerPlatform)

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
  - [Plugin Architecture](#plugin-architecture)
  - [Security Features](#security-features)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Development Setup](#development-setup)
  - [Running Locally](#running-locally)
- [Plugin Development Guide](#plugin-development-guide)
  - [Creating a Backend Plugin](#creating-a-backend-plugin)
  - [Creating a Frontend Plugin](#creating-a-frontend-plugin)
  - [Plugin Registration](#plugin-registration)
- [Deployment](#deployment)
  - [Docker Deployment](#docker-deployment)
  - [Cloud Deployment with Terraform](#cloud-deployment-with-terraform)
- [Security Considerations](#security-considerations)
- [License](#license)

## Overview

JobTriggerPlatform is a comprehensive solution for managing deployment jobs and automation workflows. It provides a secure, extensible platform that can be easily customized through its plugin architecture.

Key features:
- Plugin-based architecture for extensibility
- Role-based access control
- Secure authentication with JWT and optional 2FA
- Real-time job monitoring and notifications
- Support for multiple deployment environments
- Containerized deployment with Docker
- Infrastructure as Code with Terraform

## Architecture

The platform follows a clean architecture pattern with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│  ┌───────────┐     ┌───────────┐      ┌───────────────────┐ │
│  │  Frontend │     │  Backend  │      │  Plugin System    │ │
│  │  (React)  │◄───►│  (WebAPI) │◄────►│                   │ │
│  └───────────┘     └───────────┘      │  ┌─────────────┐  │ │
│                          │            │  │ Job Plugins │  │ │
│                          ▼            │  └─────────────┘  │ │
│                    ┌───────────┐      │                   │ │
│                    │ Database  │      │  ┌─────────────┐  │ │
│                    │(PostgreSQL)│     │  │ UI Plugins  │  │ │
│                    └───────────┘      │  └─────────────┘  │ │
│                                       └───────────────────┘ │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Plugin Architecture

The platform is built around a plugin architecture that allows for easy extension and customization:

```
┌─────────────────────┐      ┌───────────────────────────┐
│                     │      │                           │
│  Core Platform      │      │  Plugin Registry          │
│  ┌─────────────┐    │      │  ┌─────────────────────┐  │
│  │ Base        │    │      │  │ Plugin Manager      │  │
│  │ Features    │◄───┼──────┼─►│ - Registration      │  │
│  └─────────────┘    │      │  │ - Lifecycle Hooks   │  │
│                     │      │  │ - Type Safety       │  │
│                     │      │  └─────────────────────┘  │
└─────────────────────┘      │                           │
                             └───────────┬───────────────┘
                                         │
                                         ▼
┌────────────────────────────────────────────────────────────────┐
│                                                                │
│  Plugins                                                       │
│                                                                │
│  ┌───────────────┐   ┌───────────────┐   ┌───────────────┐     │
│  │ Job Stats     │   │ Notification  │   │ Single Tenant │     │
│  │ Plugin        │   │ Plugin        │   │ Plugin        │     │
│  └───────────────┘   └───────────────┘   └───────────────┘     │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

**Backend Plugins:**

The backend plugin system is based on the `IJobTriggerPlugin` interface, which allows plugins to hook into the job execution pipeline. Each plugin can define its own parameters and results, and can be registered at runtime.

**Frontend Plugins:**

The frontend plugin system uses React's dynamic loading capabilities to provide a flexible UI extension mechanism. Plugins can register components, routes, and services, and can communicate with the core application.

### Security Features

The platform includes comprehensive security features:

- **Authentication**: JWT-based authentication with optional 2FA
- **Authorization**: Role-based access control (Admin, Operator, Viewer)
- **Content Security Policy**: Strict CSP implementation with HTML sanitization
- **API Security**: Rate limiting, CSRF protection, and input validation
- **WAF Protection**: Cloud Armor integration for web application firewall protection
- **Secure Deployment**: Least privilege principles and distroless containers

## Tech Stack

**Frontend:**
- React 18 with TypeScript
- Material UI (MUI) for UI components
- React Hook Form with Zod validation
- React Router for navigation
- Axios for API communication

**Backend:**
- ASP.NET Core 8 Web API
- Entity Framework Core
- Identity with JWT authentication
- PostgreSQL for data storage
- Plugin system for extensibility

**DevOps:**
- Docker for containerization
- Terraform for infrastructure as code
- Seq for centralized logging
- Google Cloud Platform (deployment target)

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (v18 or later)
- [Docker](https://www.docker.com/get-started) and Docker Compose
- [Git](https://git-scm.com/downloads)
- [Terraform](https://www.terraform.io/downloads.html) (for cloud deployment)

### Development Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/csa7mdm/JobTriggerPlatform.git
   cd JobTriggerPlatform
   ```

2. Set up the backend:
   ```bash
   cd src/JobTriggerPlatform.WebApi
   dotnet restore
   dotnet build
   ```

3. Set up the frontend:
   ```bash
   cd ../../frontend
   npm install
   ```

### Running Locally

#### Using Docker (Recommended)

The easiest way to run the entire application is using Docker Compose:

1. From the project root, run:
   ```bash
   # On Windows
   .\docker-start.ps1
   
   # On Unix/Linux/Mac
   chmod +x docker-start.sh
   ./docker-start.sh
   ```

2. Access the application:
   - Frontend: http://localhost:80
   - Backend API: http://localhost:8080
   - Seq Logs: http://localhost:5341

#### Running Components Separately

**Backend:**
```bash
cd src/JobTriggerPlatform.WebApi
dotnet run
```

**Frontend:**
```bash
cd frontend
npm run dev
```

## Plugin Development Guide

One of the key features of JobTriggerPlatform is its extensibility through plugins. This section guides you through creating your own plugins.

### Creating a Backend Plugin

1. **Create a new Class Library project:**
   ```bash
   dotnet new classlib -n MyCustomPlugin
   ```

2. **Add references to the application interfaces:**
   ```bash
   dotnet add reference ../src/JobTriggerPlatform.Application/JobTriggerPlatform.Application.csproj
   ```

3. **Implement the `IJobTriggerPlugin` interface:**
   ```csharp
   using JobTriggerPlatform.Application.Abstractions;
   using System.Threading.Tasks;
   
   namespace MyCustomPlugin
   {
       public class MyCustomJobPlugin : IJobTriggerPlugin
       {
           public string Name => "MyCustomPlugin";
           public string Description => "A custom plugin for job triggering";
           public string Version => "1.0.0";
           
           public async Task<PluginResult> ExecuteAsync(PluginParameter[] parameters)
           {
               // Your custom logic here
               return new PluginResult
               {
                   Success = true,
                   Message = "Job executed successfully",
                   Data = new { CustomData = "Your result data" }
               };
           }
       }
   }
   ```

4. **Build your plugin:**
   ```bash
   dotnet build -c Release
   ```

5. **Copy the plugin DLL to the plugins directory:**
   ```
   /plugins/MyCustomPlugin/MyCustomPlugin.dll
   ```

### Creating a Frontend Plugin

1. **Create a new TypeScript file in the frontend plugins directory:**
   ```
   frontend/src/plugins/myCustomPlugin.ts
   ```

2. **Implement the plugin interface:**
   ```typescript
   import { lazy } from 'react';
   import { PluginDefinition } from '../types/plugin';
   
   // Lazy load your plugin component
   const MyCustomComponent = lazy(() => import('./MyCustomComponent'));
   
   export const myCustomPlugin: PluginDefinition = {
     id: 'myCustomPlugin',
     name: 'My Custom Plugin',
     description: 'A custom plugin for the deployment portal',
     version: '1.0.0',
     author: 'Your Name',
     initialize: (container) => {
       // Register your component
       container.registerComponent('my-custom-component', MyCustomComponent);
       
       // Register routes
       container.registerRoute({
         path: '/custom',
         component: MyCustomComponent,
         protected: true,
         permissions: ['admin', 'operator']
       });
       
       // Return cleanup function
       return () => {
         container.unregisterComponent('my-custom-component');
         container.unregisterRoute('/custom');
       };
     }
   };
   
   export default myCustomPlugin;
   ```

3. **Create your component:**
   ```
   frontend/src/plugins/MyCustomComponent.tsx
   ```

   ```tsx
   import React from 'react';
   import { Box, Typography, Paper } from '@mui/material';
   
   const MyCustomComponent: React.FC = () => {
     return (
       <Paper elevation={3} sx={{ p: 3, my: 2 }}>
         <Typography variant="h5" component="h2" gutterBottom>
           My Custom Plugin
         </Typography>
         <Box>
           {/* Your custom UI */}
           <Typography>This is a custom plugin component.</Typography>
         </Box>
       </Paper>
     );
   };
   
   export default MyCustomComponent;
   ```

4. **Register your plugin in main.tsx:**
   ```tsx
   // Import your plugin
   import myCustomPlugin from './plugins/myCustomPlugin';
   
   // Register plugin
   registerPlugin(myCustomPlugin, {
     autoInitialize: true
   });
   ```

### Plugin Registration

**Backend Plugin Registration:**

Plugins are automatically discovered and loaded at startup from the `plugins` directory. The `PluginLoader` class handles the loading and registration of plugins.

**Frontend Plugin Registration:**

Frontend plugins need to be registered in the `main.tsx` file:

```tsx
import { registerPlugin } from './plugins';
import myCustomPlugin from './plugins/myCustomPlugin';

// Register plugin
registerPlugin(myCustomPlugin, {
  autoInitialize: true,
  config: {
    // Plugin-specific configuration
    customOption: 'value'
  }
});
```

## Deployment

### Docker Deployment

The project includes Docker configurations for easy deployment:

1. **Build and run with Docker Compose:**
   ```bash
   docker-compose up -d
   ```

2. **Scale specific services:**
   ```bash
   docker-compose up -d --scale frontend=3
   ```

3. **Update running containers:**
   ```bash
   docker-compose up -d --build
   ```

### Cloud Deployment with Terraform

The project includes Terraform configurations for deploying to Google Cloud Platform:

1. **Initialize Terraform:**
   ```bash
   cd terraform
   terraform init
   ```

2. **Set environment variables for sensitive information:**
   ```bash
   export TF_VAR_db_password="your-secure-password"
   export TF_VAR_api_key="your-api-key"
   export TF_VAR_jwt_secret="your-jwt-secret"
   ```

3. **Plan the deployment:**
   ```bash
   terraform plan
   ```

4. **Apply the changes:**
   ```bash
   terraform apply
   ```

5. **To deploy to production instead of development:**
   ```bash
   terraform apply -var="environment=prod"
   ```

## Security Considerations

The platform implements several security best practices:

1. **Authentication and Authorization:**
   - JWT tokens with short expiration times
   - Role-based access control
   - Optional 2FA authentication

2. **Data Protection:**
   - HTTPS enforcement
   - Secure cookie handling
   - Content Security Policy implementation
   - HTML sanitization with DOMPurify

3. **API Security:**
   - Rate limiting to prevent abuse
   - CSRF protection
   - Input validation and sanitization
   - Proper error handling that doesn't leak sensitive information

4. **Infrastructure Security:**
   - Least privilege principle for service accounts
   - Private networking between services
   - WAF protection with Cloud Armor
   - Secure container configuration

## License

This project is licensed under the MIT License - see the LICENSE file for details.
