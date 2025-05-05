import axios, { AxiosResponse, AxiosError } from 'axios';

// Create an axios instance with default configuration
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add a request interceptor to handle auth tokens
apiClient.interceptors.request.use(
  (config) => {
    // You can add auth token logic here if needed
    // For example:
    // const token = localStorage.getItem('token');
    // if (token) {
    //   config.headers.Authorization = `Bearer ${token}`;
    // }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Add a response interceptor to handle common errors
apiClient.interceptors.response.use(
  (response: AxiosResponse) => {
    return response;
  },
  (error: AxiosError) => {
    if (error.response) {
      // Handle specific error status codes
      switch (error.response.status) {
        case 401:
          // Unauthorized - redirect to login
          window.location.href = '/login';
          break;
        case 403:
          // Forbidden - handle access denied
          console.error('Access denied');
          break;
        case 404:
          // Not found
          console.error('Resource not found');
          break;
        case 500:
          // Server error
          console.error('Server error');
          break;
        default:
          // Other errors
          console.error(`Request failed with status: ${error.response.status}`);
      }
    } else if (error.request) {
      // Request was made but no response was received
      console.error('No response received from server');
    } else {
      // Something happened in setting up the request
      console.error('Error setting up the request:', error.message);
    }
    return Promise.reject(error);
  }
);

// Export API functions for different resources

// Auth API
export const authApi = {
  login: (username: string, password: string) => 
    apiClient.post('/auth/login', { username, password }),
  logout: () => 
    apiClient.post('/auth/logout'),
  checkStatus: () => 
    apiClient.get('/auth/status'),
};

// Jobs API
export const jobsApi = {
  getAll: () => 
    apiClient.get('/jobs'),
  getById: (jobId: string) => 
    apiClient.get(`/jobs/${jobId}`),
  getLogs: (jobId: string) => 
    apiClient.get(`/jobs/${jobId}/logs`),
  getHistory: (jobId: string) => 
    apiClient.get(`/jobs/${jobId}/history`),
  startJob: (jobId: string) => 
    apiClient.post(`/jobs/${jobId}/start`),
  stopJob: (jobId: string) => 
    apiClient.post(`/jobs/${jobId}/stop`),
  deleteJob: (jobId: string) => 
    apiClient.delete(`/jobs/${jobId}`),
  createJob: (jobData: any) => 
    apiClient.post('/jobs', jobData),
  updateJob: (jobId: string, jobData: any) => 
    apiClient.put(`/jobs/${jobId}`, jobData),
};

// Dashboard API
export const dashboardApi = {
  getStats: () => 
    apiClient.get('/dashboard/stats'),
  getRecentJobs: () => 
    apiClient.get('/dashboard/recent-jobs'),
};

export default apiClient;