# ToDo List

## Frontend
- [x] Implement Authentication System (2025-05-01)
- [x] Create Plugin System Architecture (2025-05-02)
- [x] Develop JobStatsCard component (2025-05-03)
- [x] Implement SingleTenantForm using RHF and Zod validation (2025-05-05)
- [x] Add Admin routes (/admin/users, /admin/roles, /admin/jobs) with role-based guards (2025-05-05)
- [x] Create DataGrid-based admin pages with row editing and optimistic updates (2025-05-05)
- [x] Implement Content Security Policy with react-helmet-async (2025-05-05)
- [x] Integrate DOMPurify for HTML sanitization and SRI for CDN resources (2025-05-05)
- [ ] Create Tenant listing page
- [ ] Implement tenant edit functionality

## Backend
- [x] Refactored JobsController into JobListController (2025-05-05)
- [ ] Implement JobDetailController (in progress)
- [ ] Create TenantController with CRUD operations
- [ ] Add SQLite persistence for job history
- [ ] Implement backend API endpoints for the plugin system

## Integration
- [ ] Connect SingleTenantForm to backend API
- [ ] Implement validation feedback from server
- [ ] Add error handling and notifications

## DevOps
- [x] Create multi-stage Dockerfiles for WebApi and Frontend (2025-05-05)
- [x] Set up Docker Compose with WebApi, Frontend, PostgreSQL, and Seq (2025-05-05)
- [x] Add initialization scripts for PostgreSQL (2025-05-05)
- [x] Create startup scripts for Docker environment (2025-05-05)
- [ ] Configure CI/CD pipeline
- [ ] Set up Kubernetes deployment manifests
