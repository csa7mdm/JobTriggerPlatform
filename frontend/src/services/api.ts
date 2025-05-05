import axios, { AxiosError, AxiosRequestConfig, AxiosResponse } from 'axios'

// Create axios instance with default config
const apiClient = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

// Add request interceptor for adding auth token
apiClient.interceptors.request.use(
  (config) => {
    // Get token from local storage
    const token = localStorage.getItem('token')
    if (token) {
      config.headers['Authorization'] = `Bearer ${token}`
    }
    
    // Add CSRF token if available
    const csrfToken = getCsrfToken()
    if (csrfToken) {
      config.headers['X-XSRF-TOKEN'] = csrfToken
    }
    
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Response interceptor for handling errors
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    // Handle common error cases
    if (error.response?.status === 401) {
      // Unauthorized, clear token and redirect to login
      localStorage.removeItem('token')
      window.location.href = '/login'
    }
    
    // Rethrow the error for the service to handle
    return Promise.reject(error)
  }
)

// Helper function to get CSRF token from cookie
function getCsrfToken(): string | null {
  const name = 'XSRF-TOKEN='
  const decodedCookie = decodeURIComponent(document.cookie)
  const cookieArray = decodedCookie.split(';')
  
  for (let i = 0; i < cookieArray.length; i++) {
    let cookie = cookieArray[i].trim()
    if (cookie.indexOf(name) === 0) {
      return cookie.substring(name.length, cookie.length)
    }
  }
  
  return null
}

// Generic type for API responses
export interface ApiResponse<T> {
  data: T
  status: number
  statusText: string
  headers: any
}

// API service interface
const api = {
  // Auth
  auth: {
    login: async (credentials: { email: string; password: string }): Promise<ApiResponse<any>> => {
      const response = await apiClient.post('/auth/login', credentials)
      return response
    },
    refreshToken: async (): Promise<ApiResponse<any>> => {
      const response = await apiClient.post('/auth/refresh')
      return response
    },
    logout: async (): Promise<ApiResponse<any>> => {
      const response = await apiClient.post('/auth/logout')
      return response
    }
  },
  
  // Jobs
  jobs: {
    getAll: async (): Promise<ApiResponse<any>> => {
      const response = await apiClient.get('/jobs')
      return response
    },
    getByName: async (jobName: string): Promise<ApiResponse<any>> => {
      const response = await apiClient.get(`/jobs/${jobName}`)
      return response
    },
    trigger: async (jobName: string, parameters: Record<string, any>): Promise<ApiResponse<any>> => {
      const response = await apiClient.post(`/jobs/${jobName}`, parameters)
      return response
    }
  },
  
  // History
  history: {
    getAll: async (): Promise<ApiResponse<any>> => {
      const response = await apiClient.get('/jobs/history')
      return response
    },
    getByJobName: async (jobName: string): Promise<ApiResponse<any>> => {
      const response = await apiClient.get(`/jobs/history/${jobName}`)
      return response
    }
  },
  
  // Generic request method
  request: async <T>(config: AxiosRequestConfig): Promise<ApiResponse<T>> => {
    const response: AxiosResponse<T> = await apiClient(config)
    return response
  }
}

export default api
