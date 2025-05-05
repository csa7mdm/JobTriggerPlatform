# Deployment Portal Frontend

A modern web application for managing deployment jobs and automation tasks.

## Features

- Secure JWT authentication with HttpOnly cookies
- Role-based access control (Admin, Operator, Viewer roles)
- Resource-based permissions (job-specific access)
- Automatic token refresh
- Extensible plugin system with lazy loading
- Job management (create, view, start, stop, delete)
- Real-time job monitoring
- Job logs and history tracking
- Dashboard with key metrics and recent activities
- Theme management with light/dark mode support
- Notification system

## Tech Stack

- React 18
- TypeScript
- Vite
- Material UI 7
- React Router 7
- React Hook Form
- Zod for validation
- Axios for API requests
- MSW for API mocking
- Vitest for testing

## Getting Started

### Prerequisites

- Node.js (v18 or higher)
- pnpm (v8 or higher)

### Installation

1. Clone the repository
2. Navigate to the frontend directory
3. Install dependencies:

```bash
pnpm install
```

### Development

To start the development server:

```bash
pnpm dev
```

This will run the app in development mode with hot module replacement.
Open [http://localhost:3000](http://localhost:3000) to view it in the browser.

### Building for Production

To build the app for production:

```bash
pnpm build
```

The build output will be in the `dist` folder.

### Testing

To run the tests:

```bash
pnpm test
```

To run the tests in watch mode:

```bash
pnpm test:watch
```

## Project Structure

```
├── public/               # Public assets
├── src/                  # Source code
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
├── .env.development      # Development environment variables
├── index.html            # HTML template
├── package.json          # Project dependencies and scripts
├── tsconfig.json         # TypeScript configuration
├── vite.config.ts        # Vite configuration
└── vitest.config.ts      # Vitest configuration
```

## Authentication

This application uses JWT authentication with HttpOnly cookies for enhanced security. Key features:

- Tokens are stored in HttpOnly cookies, protected from JavaScript access
- Role-based access control (RBAC) with multiple roles: Admin, Operator, Viewer
- Resource-based permissions for job-specific access control
- Automatic token refresh every 10 minutes and when tokens are about to expire
- Axios interceptors for handling 401 responses with automatic token refresh

For more details, see the [Authentication README](./src/auth/README.md).

### Test Users

For development, the following test users are available:

| Username | Password    | Role     | Permissions                    |
|----------|-------------|----------|--------------------------------|
| admin    | password123 | admin    | Full access to all features    |
| operator | password123 | operator | Can view and run jobs          |
| viewer   | password123 | viewer   | Can only view jobs and details |

## Plugin System

The Deployment Portal includes a flexible plugin system that allows extending the application's functionality:

- Plugin registration and management
- Lazy loading via dynamic imports for better performance
- Plugin lifecycle hooks for proper initialization and cleanup
- Type-safe plugin API
- Configuration support for plugin customization

Available plugins:

- **Notifications Plugin**: Provides notification management with desktop notifications
- **Theme Plugin**: Manages application theme (light/dark) with system detection
- **Job Stats Plugin**: Advanced job statistics and analytics

For more details, see the [Plugins README](./src/plugins/README.md).

## Mock API

The application uses Mock Service Worker (MSW) for simulating API responses during development. The mock handlers are defined in `src/mocks/handlers.ts`. This allows for development without a running backend.

The mock API includes:
- Authentication endpoints with JWT token handling
- Role-based access control
- Job management endpoints
- Dashboard data endpoints
- Analytics API endpoints for the Job Stats plugin

## Available Scripts

- `pnpm dev` - Runs the app in development mode
- `pnpm build` - Builds the app for production
- `pnpm preview` - Serves the production build locally
- `pnpm lint` - Lints the code
- `pnpm format` - Formats the code with Prettier
- `pnpm test` - Runs the tests
- `pnpm test:watch` - Runs the tests in watch mode