import React, { createContext, useState, useContext, useEffect, useCallback, useRef, ReactNode } from 'react';
import axios from 'axios';
import { useNavigate, useLocation } from 'react-router-dom';

// Define the User interface with roles and job claims
interface User {
  id: string;
  username: string;
  email: string;
  roles: string[];
  allowedJobs: string[];
  exp?: number; // Expiration time
  iat?: number; // Issued at time
}

// Define the AuthContext interface
interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  hasRole: (role: string | string[]) => boolean;
  canAccessJob: (jobId: string) => boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshToken: () => Promise<boolean>;
}

// Create the AuthContext
const AuthContext = createContext<AuthContextType | undefined>(undefined);

// Define the provider props
interface AuthProviderProps {
  children: ReactNode;
}

// Create a custom hook to use the auth context
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

// Create the AuthProvider component
export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const refreshTimerRef = useRef<number | undefined>();
  const navigate = useNavigate();
  const location = useLocation();

  // Function to handle silent token refresh
  const refreshToken = useCallback(async (): Promise<boolean> => {
    try {
      // Call the refresh token endpoint
      const response = await axios.post('/api/auth/refresh-token', {}, {
        withCredentials: true // Important for cookies
      });
      
      if (response.data.user) {
        setUser(response.data.user);
        
        // Schedule the next refresh based on token expiration
        // If no expiration is provided, refresh every 10 minutes
        const tokenExp = response.data.user.exp;
        const currentTime = Math.floor(Date.now() / 1000);
        const timeToRefresh = tokenExp 
          ? (tokenExp - currentTime - 60) * 1000 // Refresh 1 minute before expiration
          : 10 * 60 * 1000; // Default 10 minutes

        // Clear any existing timer
        if (refreshTimerRef.current) {
          window.clearTimeout(refreshTimerRef.current);
        }
        
        // Set new timer
        refreshTimerRef.current = window.setTimeout(() => {
          refreshToken();
        }, Math.max(timeToRefresh, 1000)); // Ensure at least 1 second
        
        return true;
      }
      
      return false;
    } catch (error) {
      console.error('Token refresh failed:', error);
      // If refresh fails, log out the user
      setUser(null);
      return false;
    }
  }, []);

  // Check authentication status on initial load and route changes
  useEffect(() => {
    const checkAuthStatus = async () => {
      try {
        setIsLoading(true);
        
        // Silent auth check
        const response = await axios.get('/api/auth/status', {
          withCredentials: true
        });
        
        if (response.data.isAuthenticated && response.data.user) {
          setUser(response.data.user);
          
          // Setup refresh timer
          await refreshToken();
        } else {
          setUser(null);
          
          // Redirect to login page if on a protected route
          const publicPaths = ['/login', '/register', '/forgot-password'];
          if (!publicPaths.includes(location.pathname)) {
            navigate('/login', { state: { from: location.pathname } });
          }
        }
      } catch (error) {
        console.error('Auth check failed:', error);
        setUser(null);
      } finally {
        setIsLoading(false);
      }
    };

    checkAuthStatus();
    
    // Cleanup function to clear the refresh timer
    return () => {
      if (refreshTimerRef.current) {
        window.clearTimeout(refreshTimerRef.current);
      }
    };
  }, [navigate, location.pathname, refreshToken]);

  // Login function
  const login = async (username: string, password: string): Promise<void> => {
    try {
      setIsLoading(true);
      
      const response = await axios.post(
        '/api/auth/login',
        { username, password },
        { withCredentials: true }
      );
      
      if (response.data.user) {
        setUser(response.data.user);
        
        // Setup refresh timer
        await refreshToken();
        
        // Redirect to the originally requested page or dashboard
        const from = (location.state as any)?.from || '/';
        navigate(from, { replace: true });
      } else {
        throw new Error('Login failed');
      }
    } catch (error) {
      console.error('Login failed:', error);
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  // Logout function
  const logout = async (): Promise<void> => {
    try {
      setIsLoading(true);
      
      // Call the logout endpoint
      await axios.post('/api/auth/logout', {}, { 
        withCredentials: true 
      });
      
      // Clear user state and redirect to login
      setUser(null);
      
      // Clear refresh timer
      if (refreshTimerRef.current) {
        window.clearTimeout(refreshTimerRef.current);
      }
      
      navigate('/login');
    } catch (error) {
      console.error('Logout failed:', error);
      // Even if the logout request fails, clear the user state
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  };

  // Check if the user has a specific role
  const hasRole = (role: string | string[]): boolean => {
    if (!user || !user.roles) return false;
    
    if (Array.isArray(role)) {
      return role.some(r => user.roles.includes(r));
    }
    
    return user.roles.includes(role);
  };

  // Check if the user can access a specific job
  const canAccessJob = (jobId: string): boolean => {
    if (!user) return false;
    
    // If the user has the 'admin' role, they can access all jobs
    if (user.roles.includes('admin')) return true;
    
    // Check if the job is in the user's allowed jobs
    return user.allowedJobs.includes(jobId);
  };

  // Create the context value
  const value = {
    user,
    isAuthenticated: !!user,
    isLoading,
    hasRole,
    canAccessJob,
    login,
    logout,
    refreshToken,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export default AuthProvider;