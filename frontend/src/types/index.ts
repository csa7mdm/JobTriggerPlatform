// User types
export interface User {
  id: string;
  username: string;
  email: string;
  role: string;
}

// Job types
export interface Job {
  id: string;
  name: string;
  description: string;
  status: JobStatus;
  lastRun: string | null;
  nextRun: string | null;
  createdAt: string;
}

export type JobStatus = 'idle' | 'running' | 'success' | 'failed';

export interface JobDetails extends Job {
  createdBy: string;
  command: string;
  timeout: number;
  maxRetries: number;
  retryDelay: number;
  environment: string;
  tags: string[];
}

export interface LogEntry {
  id: string;
  timestamp: string;
  level: LogLevel;
  message: string;
}

export type LogLevel = 'info' | 'warning' | 'error';

export interface RunHistory {
  id: string;
  startTime: string;
  endTime: string | null;
  status: JobStatus;
  duration: string | null;
  triggeredBy: string;
}

// Dashboard types
export interface DashboardStats {
  totalJobs: number;
  activeJobs: number;
  failedJobs: number;
  successfulJobs: number;
  pendingJobs: number;
}

// API response types
export interface ApiResponse<T> {
  data: T;
  message?: string;
  success: boolean;
}

export interface PaginatedResponse<T> {
  data: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}