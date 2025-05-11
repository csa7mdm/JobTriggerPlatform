import apiClient from './client';
import { Tenant, CreateTenantDto, UpdateTenantDto } from '../types/tenant';

/**
 * Tenant API client
 */
export const tenantApi = {
  /**
   * Get all tenants
   * @returns Promise with all tenants
   */
  getAll: () => 
    apiClient.get<Tenant[]>('/api/tenants'),
  
  /**
   * Get a tenant by ID
   * @param id Tenant ID
   * @returns Promise with tenant details
   */
  getById: (id: string) => 
    apiClient.get<Tenant>(`/api/tenants/${id}`),
  
  /**
   * Create a new tenant
   * @param data Tenant data
   * @returns Promise with created tenant
   */
  create: (data: CreateTenantDto) => 
    apiClient.post<Tenant>('/api/tenants', data),
  
  /**
   * Update an existing tenant
   * @param id Tenant ID
   * @param data Updated tenant data
   * @returns Promise with updated tenant
   */
  update: (id: string, data: UpdateTenantDto) => 
    apiClient.put<Tenant>(`/api/tenants/${id}`, data),
  
  /**
   * Delete a tenant
   * @param id Tenant ID
   * @returns Promise with delete operation result
   */
  delete: (id: string) => 
    apiClient.delete(`/api/tenants/${id}`),
  
  /**
   * Generate a new API key for a tenant
   * @param id Tenant ID
   * @returns Promise with the new API key
   */
  generateApiKey: (id: string) => 
    apiClient.post<{ apiKey: string }>(`/api/tenants/${id}/api-key`),
  
  /**
   * Revoke the current API key for a tenant
   * @param id Tenant ID
   * @returns Promise with operation result
   */
  revokeApiKey: (id: string) => 
    apiClient.delete(`/api/tenants/${id}/api-key`),
};

export default tenantApi;
