/**
 * Represents a tenant in the system.
 */
export interface Tenant {
  id: string;
  name: string;
  description: string;
  contactEmail: string;
  contactName: string;
  contactPhone?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  allowedJobs: string[]; // IDs of jobs this tenant is allowed to access
  apiKey?: string; // API key for tenant access (may be null if not generated yet)
  settings?: TenantSettings;
}

/**
 * Tenant settings for customization.
 */
export interface TenantSettings {
  logoUrl?: string;
  primaryColor?: string;
  allowApiAccess: boolean;
  maxConcurrentJobs: number;
  notificationEmail?: string;
  webhookUrl?: string;
}

/**
 * Data transfer object for creating a new tenant.
 */
export interface CreateTenantDto {
  name: string;
  description: string;
  contactEmail: string;
  contactName: string;
  contactPhone?: string;
  isActive: boolean;
  allowedJobs: string[];
  settings?: TenantSettings;
}

/**
 * Data transfer object for updating an existing tenant.
 */
export interface UpdateTenantDto {
  name?: string;
  description?: string;
  contactEmail?: string;
  contactName?: string;
  contactPhone?: string;
  isActive?: boolean;
  allowedJobs?: string[];
  settings?: TenantSettings;
}
