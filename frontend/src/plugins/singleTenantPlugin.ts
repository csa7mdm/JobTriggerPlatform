import { lazy } from 'react';
import { PluginDefinition } from '../types/plugin';

const SingleTenantForm = lazy(() => import('./singleTenant'));

export const singleTenantPlugin: PluginDefinition = {
  id: 'singleTenant',
  name: 'Single Tenant Management',
  description: 'Manage single tenant configurations',
  version: '1.0.0',
  author: 'Deployment Portal Team',
  initialize: (container) => {
    container.registerComponent('tenant-form', SingleTenantForm);
    
    // Register any routes or services if needed
    container.registerRoute({
      path: '/tenants/manage',
      component: SingleTenantForm,
      protected: true,
      permissions: ['admin']
    });
    
    // Return cleanup function
    return () => {
      container.unregisterComponent('tenant-form');
      container.unregisterRoute('/tenants/manage');
    };
  }
};

export default singleTenantPlugin;