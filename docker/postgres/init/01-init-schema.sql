-- Create schema and initial tables for JobTriggerPlatform

-- Create extension for UUID generation
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create Users table
CREATE TABLE "Users" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Username" VARCHAR(100) NOT NULL UNIQUE,
    "Email" VARCHAR(255) NOT NULL UNIQUE,
    "FirstName" VARCHAR(100),
    "LastName" VARCHAR(100),
    "PasswordHash" VARCHAR(255) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Create Roles table
CREATE TABLE "Roles" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Name" VARCHAR(50) NOT NULL UNIQUE,
    "Description" VARCHAR(255),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Create User Roles junction table
CREATE TABLE "UserRoles" (
    "UserId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "RoleId" UUID NOT NULL REFERENCES "Roles"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("UserId", "RoleId")
);

-- Create Permissions table
CREATE TABLE "Permissions" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Name" VARCHAR(100) NOT NULL UNIQUE,
    "Description" VARCHAR(255),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Create Role Permissions junction table
CREATE TABLE "RolePermissions" (
    "RoleId" UUID NOT NULL REFERENCES "Roles"("Id") ON DELETE CASCADE,
    "PermissionId" UUID NOT NULL REFERENCES "Permissions"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("RoleId", "PermissionId")
);

-- Create Jobs table
CREATE TABLE "Jobs" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Name" VARCHAR(100) NOT NULL UNIQUE,
    "Description" TEXT,
    "Command" TEXT NOT NULL,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'idle',
    "Schedule" VARCHAR(100), -- Cron expression
    "Timeout" INTEGER NOT NULL DEFAULT 3600, -- in seconds
    "MaxRetries" INTEGER NOT NULL DEFAULT 3,
    "RetryDelay" INTEGER NOT NULL DEFAULT 60, -- in seconds
    "Environment" VARCHAR(50) NOT NULL DEFAULT 'development',
    "CreatedBy" UUID REFERENCES "Users"("Id"),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "LastRunAt" TIMESTAMP,
    "NextRunAt" TIMESTAMP,
    "IsEnabled" BOOLEAN NOT NULL DEFAULT TRUE
);

-- Create Job Tags table
CREATE TABLE "JobTags" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Name" VARCHAR(50) NOT NULL UNIQUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Create Jobs-Tags junction table
CREATE TABLE "JobTagsMap" (
    "JobId" UUID NOT NULL REFERENCES "Jobs"("Id") ON DELETE CASCADE,
    "TagId" UUID NOT NULL REFERENCES "JobTags"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("JobId", "TagId")
);

-- Create Job History table
CREATE TABLE "JobHistory" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "JobId" UUID NOT NULL REFERENCES "Jobs"("Id") ON DELETE CASCADE,
    "StartTime" TIMESTAMP NOT NULL DEFAULT NOW(),
    "EndTime" TIMESTAMP,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'running',
    "TriggeredBy" UUID REFERENCES "Users"("Id"),
    "TriggeredBySystem" BOOLEAN NOT NULL DEFAULT FALSE,
    "ExitCode" INTEGER,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Create Job Logs table
CREATE TABLE "JobLogs" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "JobId" UUID NOT NULL REFERENCES "Jobs"("Id") ON DELETE CASCADE,
    "JobHistoryId" UUID REFERENCES "JobHistory"("Id") ON DELETE CASCADE,
    "Timestamp" TIMESTAMP NOT NULL DEFAULT NOW(),
    "Level" VARCHAR(20) NOT NULL,
    "Message" TEXT NOT NULL
);

-- Create Tenants table
CREATE TABLE "Tenants" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "TenantId" VARCHAR(50) NOT NULL UNIQUE,
    "DisplayName" VARCHAR(100) NOT NULL,
    "Description" TEXT,
    "ConnectionString" TEXT NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "MaxConcurrentJobs" INTEGER NOT NULL DEFAULT 5,
    "ApiKey" VARCHAR(100),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Create indexes for better performance
CREATE INDEX "idx_jobs_status" ON "Jobs"("Status");
CREATE INDEX "idx_jobs_created_at" ON "Jobs"("CreatedAt");
CREATE INDEX "idx_job_history_job_id" ON "JobHistory"("JobId");
CREATE INDEX "idx_job_history_status" ON "JobHistory"("Status");
CREATE INDEX "idx_job_logs_job_id" ON "JobLogs"("JobId");
CREATE INDEX "idx_job_logs_timestamp" ON "JobLogs"("Timestamp");
CREATE INDEX "idx_job_logs_job_history_id" ON "JobLogs"("JobHistoryId");

-- Insert default roles
INSERT INTO "Roles" ("Name", "Description") VALUES
    ('admin', 'Administrator with full access'),
    ('operator', 'Operator who can run jobs'),
    ('viewer', 'User with read-only access');

-- Insert default permissions
INSERT INTO "Permissions" ("Name", "Description") VALUES
    ('jobs.view', 'Can view jobs'),
    ('jobs.create', 'Can create jobs'),
    ('jobs.edit', 'Can edit jobs'),
    ('jobs.delete', 'Can delete jobs'),
    ('jobs.run', 'Can run jobs'),
    ('jobs.stop', 'Can stop jobs'),
    ('users.view', 'Can view users'),
    ('users.create', 'Can create users'),
    ('users.edit', 'Can edit users'),
    ('users.delete', 'Can delete users'),
    ('roles.view', 'Can view roles'),
    ('roles.create', 'Can create roles'),
    ('roles.edit', 'Can edit roles'),
    ('roles.delete', 'Can delete roles'),
    ('tenants.view', 'Can view tenants'),
    ('tenants.create', 'Can create tenants'),
    ('tenants.edit', 'Can edit tenants'),
    ('tenants.delete', 'Can delete tenants');

-- Assign permissions to roles
-- Admin role - all permissions
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
SELECT r."Id", p."Id"
FROM "Roles" r, "Permissions" p
WHERE r."Name" = 'admin';

-- Operator role - view, run, stop jobs
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
SELECT r."Id", p."Id"
FROM "Roles" r, "Permissions" p
WHERE r."Name" = 'operator' AND p."Name" IN ('jobs.view', 'jobs.run', 'jobs.stop', 'tenants.view');

-- Viewer role - view jobs only
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
SELECT r."Id", p."Id"
FROM "Roles" r, "Permissions" p
WHERE r."Name" = 'viewer' AND p."Name" IN ('jobs.view', 'tenants.view');

-- Create admin user (password: admin123)
INSERT INTO "Users" ("Username", "Email", "FirstName", "LastName", "PasswordHash")
VALUES ('admin', 'admin@example.com', 'Admin', 'User', '$2a$11$Uj7EBSKNIRJJrI6JxU6tXOv/IVY.1AFPkJ1L.8YCSR2XxZDYjW3Wq');

-- Assign admin role to admin user
INSERT INTO "UserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "Users" u, "Roles" r
WHERE u."Username" = 'admin' AND r."Name" = 'admin';

-- Sample Job Tags
INSERT INTO "JobTags" ("Name") VALUES
    ('deployment'),
    ('backup'),
    ('database'),
    ('maintenance'),
    ('reporting'),
    ('production'),
    ('staging'),
    ('development');

-- Create sample jobs
INSERT INTO "Jobs" ("Name", "Description", "Command", "Status", "Schedule", "Environment", "CreatedBy")
SELECT 
    'Production Deploy', 
    'Deploy to production environment', 
    'bash scripts/deploy.sh --env=production',
    'idle',
    '0 9 * * 1-5', -- 9 AM on weekdays
    'production',
    u."Id"
FROM "Users" u
WHERE u."Username" = 'admin';

INSERT INTO "Jobs" ("Name", "Description", "Command", "Status", "Schedule", "Environment", "CreatedBy")
SELECT 
    'Database Backup', 
    'Daily backup of the production database', 
    'bash scripts/backup-db.sh --full --compress',
    'idle',
    '0 1 * * *', -- 1 AM every day
    'production',
    u."Id"
FROM "Users" u
WHERE u."Username" = 'admin';

-- Assign tags to sample jobs
INSERT INTO "JobTagsMap" ("JobId", "TagId")
SELECT j."Id", t."Id"
FROM "Jobs" j, "JobTags" t
WHERE j."Name" = 'Production Deploy' AND t."Name" = 'deployment';

INSERT INTO "JobTagsMap" ("JobId", "TagId")
SELECT j."Id", t."Id"
FROM "Jobs" j, "JobTags" t
WHERE j."Name" = 'Production Deploy' AND t."Name" = 'production';

INSERT INTO "JobTagsMap" ("JobId", "TagId")
SELECT j."Id", t."Id"
FROM "Jobs" j, "JobTags" t
WHERE j."Name" = 'Database Backup' AND t."Name" = 'backup';

INSERT INTO "JobTagsMap" ("JobId", "TagId")
SELECT j."Id", t."Id"
FROM "Jobs" j, "JobTags" t
WHERE j."Name" = 'Database Backup' AND t."Name" = 'database';

INSERT INTO "JobTagsMap" ("JobId", "TagId")
SELECT j."Id", t."Id"
FROM "Jobs" j, "JobTags" t
WHERE j."Name" = 'Database Backup' AND t."Name" = 'production';