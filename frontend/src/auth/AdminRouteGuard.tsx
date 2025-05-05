import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from './index';

interface AdminRouteGuardProps {
  children: React.ReactNode;
}

const AdminRouteGuard: React.FC<AdminRouteGuardProps> = ({ children }) => {
  const { user, isAuthenticated, isLoading } = useAuth();

  // If still loading, render nothing or a loading spinner
  if (isLoading) {
    return <div>Loading...</div>;
  }

  // If not authenticated, redirect to login
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  // If authenticated but not admin, redirect to unauthorized
  if (!user?.roles?.includes('admin')) {
    return <Navigate to="/unauthorized" replace />;
  }

  // If authenticated and admin, render the protected content
  return <>{children}</>;
};

export default AdminRouteGuard;