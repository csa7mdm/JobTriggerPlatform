import axios from 'axios';

// Function to parse JWT token (for debugging only)
// Note: In normal operation, the token is stored in HttpOnly cookies
// and shouldn't be accessible directly in JavaScript
export const parseJwt = (token: string) => {
  try {
    // Split the token and get the payload part
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );

    return JSON.parse(jsonPayload);
  } catch (error) {
    console.error('Failed to parse JWT token:', error);
    return null;
  }
};

// Configure axios to handle 401 responses by refreshing the token
export const setupAxiosInterceptors = (refreshToken: () => Promise<boolean>) => {
  // Add a response interceptor
  axios.interceptors.response.use(
    response => response,
    async error => {
      const originalRequest = error.config;
      
      // If the error is due to an unauthorized request (401) and
      // we haven't tried to refresh the token yet
      if (error.response?.status === 401 && !originalRequest._retry) {
        originalRequest._retry = true;
        
        try {
          // Try to refresh the token
          const refreshSuccessful = await refreshToken();
          
          if (refreshSuccessful) {
            // If refresh was successful, retry the original request
            return axios(originalRequest);
          }
          
          // If refresh was not successful, throw the original error
          return Promise.reject(error);
        } catch (refreshError) {
          // If token refresh failed, return the original error
          return Promise.reject(error);
        }
      }
      
      // For other errors, just return the error
      return Promise.reject(error);
    }
  );
};

// Check if a token is expired (for debugging)
export const isTokenExpired = (exp?: number): boolean => {
  if (!exp) return true;
  
  const currentTime = Math.floor(Date.now() / 1000);
  return currentTime >= exp;
};

// Function to safely get user data from a token
export const extractUserFromToken = (token?: string) => {
  if (!token) return null;
  
  try {
    const decoded = parseJwt(token);
    
    if (!decoded || isTokenExpired(decoded.exp)) {
      return null;
    }
    
    return {
      id: decoded.sub || decoded.id,
      username: decoded.username,
      email: decoded.email,
      roles: decoded.roles || [],
      allowedJobs: decoded.allowedJobs || [],
      exp: decoded.exp,
      iat: decoded.iat
    };
  } catch (error) {
    console.error('Failed to extract user from token:', error);
    return null;
  }
};