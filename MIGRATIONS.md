# Database Migrations Guide

This document provides instructions for creating and applying database migrations in the JobTriggerPlatform project.

## Prerequisites

- .NET SDK 9.0 or later
- Entity Framework Core tools (`dotnet ef`)

If you don't have the Entity Framework Core tools installed, you can install them using the following command:

```bash
dotnet tool install --global dotnet-ef
```

## Creating a Migration

To create a new migration, run the following command from the `src/JobTriggerPlatform.WebApi` directory:

```bash
dotnet ef migrations add [MigrationName] --project ../JobTriggerPlatform.Infrastructure --startup-project .
```

Replace `[MigrationName]` with a descriptive name for your migration, for example:

```bash
dotnet ef migrations add InitialCreate --project ../JobTriggerPlatform.Infrastructure --startup-project .
```

## Applying Migrations Manually

The application is configured to automatically apply pending migrations when it starts up. However, if you need to apply migrations manually, you can use the following command:

```bash
dotnet ef database update --project ../JobTriggerPlatform.Infrastructure --startup-project .
```

## Removing the Last Migration

If you need to remove the last migration (that hasn't been applied to the database), use:

```bash
dotnet ef migrations remove --project ../JobTriggerPlatform.Infrastructure --startup-project .
```

## Generating a SQL Script

To generate a SQL script for the migrations, use:

```bash
dotnet ef migrations script --project ../JobTriggerPlatform.Infrastructure --startup-project .
```

This will generate a SQL script that you can use to apply migrations manually if needed.

## Creating the Initial Migration

For first-time setup, run:

```bash
dotnet ef migrations add InitialCreate --project ../JobTriggerPlatform.Infrastructure --startup-project .
```

This creates the initial migration with all the tables needed for the application.
