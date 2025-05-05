import { Routes, Route, Navigate } from 'react-router-dom'
import Layout from './components/Layout'
import Dashboard from './pages/Dashboard'
import Jobs from './pages/Jobs'
import JobDetail from './pages/JobDetail'
import Login from './pages/Login'
import NotFound from './pages/NotFound'
import Unauthorized from './pages/Unauthorized'
import { ProtectedRoute, AdminRouteGuard } from './auth'
import { useEffect } from 'react'
import { setupAxiosInterceptors } from './auth/authUtils'
import { useAuth } from './auth'

// Admin pages
import UsersAdmin from './pages/admin/UsersAdmin'
import RolesAdmin from './pages/admin/RolesAdmin'
import JobsAdmin from './pages/admin/JobsAdmin'

// Constants
import { ROUTES } from './config/constants'

function App() {
  const { refreshToken } = useAuth();

  // Set up axios interceptors for automatic token refresh
  useEffect(() => {
    setupAxiosInterceptors(refreshToken);
  }, [refreshToken]);

  return (
    <Routes>
      {/* Public routes */}
      <Route path="/login" element={<Login />} />
      <Route path="/unauthorized" element={<Unauthorized />} />
      
      {/* Protected Routes with Layout */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <Layout />
          </ProtectedRoute>
        }
      >
        {/* Dashboard - accessible to all authenticated users */}
        <Route index element={<Dashboard />} />
        
        {/* Jobs list - requires specific roles */}
        <Route
          path="jobs"
          element={
            <ProtectedRoute requiredRoles={['admin', 'operator', 'viewer']}>
              <Jobs />
            </ProtectedRoute>
          }
        />
        
        {/* Job detail - uses job-specific access control in the component */}
        <Route
          path="jobs/:jobName"
          element={
            <ProtectedRoute>
              <JobDetail />
            </ProtectedRoute>
          }
        />
        
        {/* Catch all unknown routes */}
        {/* Admin Routes */}
        <Route
          path={ROUTES.ADMIN.USERS}
          element={
            <AdminRouteGuard>
              <UsersAdmin />
            </AdminRouteGuard>
          }
        />
        
        <Route
          path={ROUTES.ADMIN.ROLES}
          element={
            <AdminRouteGuard>
              <RolesAdmin />
            </AdminRouteGuard>
          }
        />
        
        <Route
          path={ROUTES.ADMIN.JOBS}
          element={
            <AdminRouteGuard>
              <JobsAdmin />
            </AdminRouteGuard>
          }
        />
        
        <Route path="*" element={<NotFound />} />
      </Route>
    </Routes>
  )
}

export default App