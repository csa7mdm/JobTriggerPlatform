// API Constants
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

// Authentication
export const TOKEN_REFRESH_INTERVAL = 1000 * 60 * 10; // 10 minutes

// Application settings
export const APP_NAME = 'Deployment Portal';
export const APP_VERSION = '1.0.0';

// Routes
export const ROUTES = {
  HOME: '/',
  LOGIN: '/login',
  JOBS: '/jobs',
  JOB_DETAIL: '/jobs/:jobName',
  ADMIN: {
    USERS: '/admin/users',
    ROLES: '/admin/roles',
    JOBS: '/admin/jobs',
  },
  UNAUTHORIZED: '/unauthorized'
};

// Roles and Permissions
export const ROLES = {
  ADMIN: 'admin',
  OPERATOR: 'operator',
  VIEWER: 'viewer'
};

// Plugin related constants
export const PLUGIN_SETTINGS = {
  MAX_NOTIFICATIONS: 50,
  POLLING_INTERVAL: 30000, // 30 seconds
};

export default {
  API_BASE_URL,
  TOKEN_REFRESH_INTERVAL,
  APP_NAME,
  APP_VERSION,
  ROUTES,
  ROLES,
  PLUGIN_SETTINGS
};