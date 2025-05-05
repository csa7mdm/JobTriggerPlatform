import { http, HttpResponse, delay } from 'msw';
import { generateId } from '../utils/helpers';
import { analyticsHandlers } from './analytics-handlers';

// Mock User Data with roles and job permissions
const users = [
  {
    id: '1',
    username: 'admin',
    email: 'admin@example.com',
    roles: ['admin'],
    allowedJobs: ['*'], // Admin can access all jobs
    password: 'password123', // In a real app, this would be hashed
  },
  {
    id: '2',
    username: 'operator',
    email: 'operator@example.com',
    roles: ['operator'],
    allowedJobs: ['1', '2', '4'], // Allowed jobs by ID
    password: 'password123',
  },
  {
    id: '3',
    username: 'viewer',
    email: 'viewer@example.com',
    roles: ['viewer'],
    allowedJobs: ['1', '2', '3', '4', '5'], // Can view all jobs but not modify
    password: 'password123',
  },
];

// Mock Jobs Data
let jobs = [
  {
    id: '1',
    name: 'Production Deploy',
    description: 'Deploy to production environment',
    status: 'success',
    lastRun: '2025-05-05 09:30:00',
    nextRun: '2025-05-06 09:30:00',
    createdAt: '2025-01-01 00:00:00',
    createdBy: 'admin@example.com',
    command: 'bash scripts/deploy.sh --env=production',
    timeout: 3600,
    maxRetries: 3,
    retryDelay: 60,
    environment: 'production',
    tags: ['deploy', 'production'],
  },
  {
    id: '2',
    name: 'Database Backup',
    description: 'Backup all production databases',
    status: 'success',
    lastRun: '2025-05-05 08:00:00',
    nextRun: '2025-05-06 08:00:00',
    createdAt: '2025-01-02 00:00:00',
    createdBy: 'admin@example.com',
    command: 'bash scripts/backup.sh --database=all',
    timeout: 1800,
    maxRetries: 2,
    retryDelay: 30,
    environment: 'production',
    tags: ['backup', 'database'],
  },
  {
    id: '3',
    name: 'Test Environment Reset',
    description: 'Reset test environment to clean state',
    status: 'failed',
    lastRun: '2025-05-04 23:00:00',
    nextRun: '2025-05-05 23:00:00',
    createdAt: '2025-01-03 00:00:00',
    createdBy: 'admin@example.com',
    command: 'bash scripts/reset.sh --env=test',
    timeout: 900,
    maxRetries: 1,
    retryDelay: 15,
    environment: 'test',
    tags: ['reset', 'test'],
  },
  {
    id: '4',
    name: 'Integration Tests',
    description: 'Run all integration tests',
    status: 'running',
    lastRun: '2025-05-05 10:15:00',
    nextRun: null,
    createdAt: '2025-01-04 00:00:00',
    createdBy: 'admin@example.com',
    command: 'npm run test:integration',
    timeout: 1200,
    maxRetries: 0,
    retryDelay: 0,
    environment: 'test',
    tags: ['test', 'integration'],
  },
  {
    id: '5',
    name: 'Code Analysis',
    description: 'Run static code analysis',
    status: 'idle',
    lastRun: '2025-05-03 12:00:00',
    nextRun: '2025-05-06 12:00:00',
    createdAt: '2025-01-05 00:00:00',
    createdBy: 'admin@example.com',
    command: 'npm run lint',
    timeout: 600,
    maxRetries: 0,
    retryDelay: 0,
    environment: 'development',
    tags: ['lint', 'analysis'],
  },
];

// Mock Dashboard Stats
const dashboardStats = {
  totalJobs: jobs.length,
  activeJobs: jobs.filter(job => job.status === 'running').length,
  failedJobs: jobs.filter(job => job.status === 'failed').length,
  successfulJobs: jobs.filter(job => job.status === 'success').length,
  pendingJobs: jobs.filter(job => job.status === 'idle').length,
};

// Mock JWT token handling
const generateToken = (user: any) => {
  // In a real app, this would be a JWT signed with a secret key
  // For our mock purposes, we'll just create a fake token
  const { password, ...userWithoutPassword } = user;
  
  // Add expiration and issued at times
  const tokenData = {
    ...userWithoutPassword,
    exp: Math.floor(Date.now() / 1000) + 3600, // Expire in 1 hour
    iat: Math.floor(Date.now() / 1000),
  };
  
  // Normally we'd sign this, but for mocking we'll just encode it
  return btoa(JSON.stringify(tokenData));
};

// Mock Logs
const generateLogs = (jobId: string) => {
  const logs = [];
  
  if (jobId === '1') {
    logs.push(
      {
        id: '1',
        timestamp: '2025-05-05 09:30:00',
        level: 'info',
        message: 'Starting deployment process',
      },
      {
        id: '2',
        timestamp: '2025-05-05 09:30:05',
        level: 'info',
        message: 'Pulling latest changes from repository',
      },
      {
        id: '3',
        timestamp: '2025-05-05 09:30:20',
        level: 'info',
        message: 'Building application',
      },
      {
        id: '4',
        timestamp: '2025-05-05 09:32:45',
        level: 'warning',
        message: 'Slow build time detected',
      },
      {
        id: '5',
        timestamp: '2025-05-05 09:33:30',
        level: 'info',
        message: 'Build completed successfully',
      },
      {
        id: '6',
        timestamp: '2025-05-05 09:33:45',
        level: 'info',
        message: 'Starting deployment to servers',
      },
      {
        id: '7',
        timestamp: '2025-05-05 09:34:15',
        level: 'info',
        message: 'Deployment completed successfully',
      }
    );
  } else if (jobId === '3') {
    logs.push(
      {
        id: '1',
        timestamp: '2025-05-04 23:00:00',
        level: 'info',
        message: 'Starting test environment reset',
      },
      {
        id: '2',
        timestamp: '2025-05-04 23:00:10',
        level: 'info',
        message: 'Stopping services',
      },
      {
        id: '3',
        timestamp: '2025-05-04 23:00:30',
        level: 'info',
        message: 'Clearing database',
      },
      {
        id: '4',
        timestamp: '2025-05-04 23:01:15',
        level: 'error',
        message: 'Failed to clear database: Connection refused',
      },
      {
        id: '5',
        timestamp: '2025-05-04 23:01:30',
        level: 'error',
        message: 'Reset process failed',
      }
    );
  } else {
    logs.push(
      {
        id: '1',
        timestamp: '2025-05-05 08:00:00',
        level: 'info',
        message: 'Starting job',
      },
      {
        id: '2',
        timestamp: '2025-05-05 08:01:00',
        level: 'info',
        message: 'Job completed',
      }
    );
  }
  
  return logs;
};

// Mock Run History
const generateRunHistory = (jobId: string) => {
  const history = [];
  
  if (jobId === '1') {
    history.push(
      {
        id: '1',
        startTime: '2025-05-05 09:30:00',
        endTime: '2025-05-05 09:34:15',
        status: 'success',
        duration: '4m 15s',
        triggeredBy: 'admin@example.com',
      },
      {
        id: '2',
        startTime: '2025-05-04 09:30:00',
        endTime: '2025-05-04 09:33:22',
        status: 'success',
        duration: '3m 22s',
        triggeredBy: 'scheduler',
      },
      {
        id: '3',
        startTime: '2025-05-03 09:30:00',
        endTime: '2025-05-03 09:31:45',
        status: 'failed',
        duration: '1m 45s',
        triggeredBy: 'scheduler',
      }
    );
  } else if (jobId === '3') {
    history.push(
      {
        id: '1',
        startTime: '2025-05-04 23:00:00',
        endTime: '2025-05-04 23:01:30',
        status: 'failed',
        duration: '1m 30s',
        triggeredBy: 'scheduler',
      },
      {
        id: '2',
        startTime: '2025-05-03 23:00:00',
        endTime: '2025-05-03 23:02:10',
        status: 'success',
        duration: '2m 10s',
        triggeredBy: 'scheduler',
      }
    );
  } else if (jobId === '4') {
    history.push(
      {
        id: '1',
        startTime: '2025-05-05 10:15:00',
        endTime: null,
        status: 'running',
        duration: null,
        triggeredBy: 'admin@example.com',
      }
    );
  } else {
    history.push(
      {
        id: '1',
        startTime: '2025-05-03 12:00:00',
        endTime: '2025-05-03 12:05:45',
        status: 'success',
        duration: '5m 45s',
        triggeredBy: 'scheduler',
      }
    );
  }
  
  return history;
};

// Helper function to get user from cookie in request
const getUserFromCookies = (request: Request) => {
  const cookies = request.headers.get('cookie') || '';
  
  // In a real app, we'd parse the JWT token from cookies
  // For this mock, we'll assume the user is always authenticated as admin
  return users[0];
};

// Combined handlers
export const handlers = [
  // Auth API
  http.post('/api/auth/login', async ({ request }) => {
    await delay(800); // Simulate network delay
    
    const { username, password } = await request.json();
    
    // Find the user
    const user = users.find(u => u.username === username && u.password === password);
    
    if (user) {
      // Generate a token
      const token = generateToken(user);
      
      // Remove password from user object
      const { password, ...userWithoutPassword } = user;
      
      // Return the user data and set a cookie
      return new HttpResponse(
        JSON.stringify({
          user: userWithoutPassword,
          isAuthenticated: true,
        }),
        {
          status: 200,
          headers: {
            'Content-Type': 'application/json',
            // Set the token as an HTTP-only cookie
            'Set-Cookie': `auth-token=${token}; Path=/; HttpOnly; SameSite=Strict;`,
          },
        }
      );
    }
    
    return new HttpResponse(
      JSON.stringify({
        message: 'Invalid username or password',
        isAuthenticated: false,
      }),
      {
        status: 401,
        headers: {
          'Content-Type': 'application/json',
        },
      }
    );
  }),
  
  http.post('/api/auth/logout', async () => {
    await delay(300);
    
    // Clear the auth cookie
    return new HttpResponse(
      JSON.stringify({
        success: true,
        message: 'Logged out successfully',
      }),
      {
        status: 200,
        headers: {
          'Content-Type': 'application/json',
          // Clear the auth cookie
          'Set-Cookie': 'auth-token=; Path=/; HttpOnly; SameSite=Strict; Expires=Thu, 01 Jan 1970 00:00:00 GMT;',
        },
      }
    );
  }),
  
  http.get('/api/auth/status', async ({ request }) => {
    await delay(500);
    
    // Get user from cookies
    const user = getUserFromCookies(request);
    
    if (user) {
      // Remove password from user object
      const { password, ...userWithoutPassword } = user;
      
      return HttpResponse.json({
        isAuthenticated: true,
        user: userWithoutPassword,
      });
    }
    
    return HttpResponse.json({
      isAuthenticated: false,
      user: null,
    });
  }),
  
  http.post('/api/auth/refresh-token', async ({ request }) => {
    await delay(300);
    
    // Get the user from cookies
    const user = getUserFromCookies(request);
    
    if (user) {
      // Generate a new token
      const token = generateToken(user);
      
      // Remove password from user object
      const { password, ...userWithoutPassword } = user;
      
      return new HttpResponse(
        JSON.stringify({
          user: userWithoutPassword,
          isAuthenticated: true,
        }),
        {
          status: 200,
          headers: {
            'Content-Type': 'application/json',
            // Set the new token as an HTTP-only cookie
            'Set-Cookie': `auth-token=${token}; Path=/; HttpOnly; SameSite=Strict;`,
          },
        }
      );
    }
    
    return new HttpResponse(
      JSON.stringify({
        message: 'Invalid token',
        isAuthenticated: false,
      }),
      {
        status: 401,
        headers: {
          'Content-Type': 'application/json',
        },
      }
    );
  }),
  
  // Jobs API
  http.get('/api/jobs', async ({ request }) => {
    await delay(500); // Simulate network delay
    
    // Get user from cookies for access control
    const user = getUserFromCookies(request);
    
    if (!user) {
      return new HttpResponse(null, { status: 401 });
    }
    
    // Filter jobs based on user permissions
    let accessibleJobs = jobs;
    
    // If not admin (who can access all jobs), filter by allowed jobs
    if (!user.roles.includes('admin')) {
      accessibleJobs = jobs.filter(job => 
        user.allowedJobs.includes(job.id) || user.allowedJobs.includes('*')
      );
    }
    
    return HttpResponse.json(accessibleJobs);
  }),
  
  http.get('/api/jobs/:jobId', async ({ params, request }) => {
    await delay(300);
    
    const { jobId } = params;
    const job = jobs.find(j => j.id === jobId);
    
    // Get user from cookies for access control
    const user = getUserFromCookies(request);
    
    if (!user) {
      return new HttpResponse(null, { status: 401 });
    }
    
    // Check if user has access to this job
    const hasAccess = 
      user.roles.includes('admin') || 
      user.allowedJobs.includes('*') || 
      user.allowedJobs.includes(jobId as string);
    
    if (!hasAccess) {
      return new HttpResponse(null, { status: 403 });
    }
    
    if (job) {
      return HttpResponse.json(job);
    }
    
    return new HttpResponse(null, {
      status: 404,
      statusText: 'Not Found',
    });
  }),
  
  http.get('/api/jobs/:jobId/logs', async ({ params, request }) => {
    await delay(300);
    
    const { jobId } = params;
    
    // Get user from cookies for access control
    const user = getUserFromCookies(request);
    
    if (!user) {
      return new HttpResponse(null, { status: 401 });
    }
    
    // Check if user has access to this job
    const hasAccess = 
      user.roles.includes('admin') || 
      user.allowedJobs.includes('*') || 
      user.allowedJobs.includes(jobId as string);
    
    if (!hasAccess) {
      return new HttpResponse(null, { status: 403 });
    }
    
    const job = jobs.find(j => j.id === jobId);
    
    if (job) {
      return HttpResponse.json(generateLogs(job.id));
    }
    
    return new HttpResponse(null, {
      status: 404,
      statusText: 'Not Found',
    });
  }),
  
  http.get('/api/jobs/:jobId/history', async ({ params, request }) => {
    await delay(300);
    
    const { jobId } = params;
    
    // Get user from cookies for access control
    const user = getUserFromCookies(request);
    
    if (!user) {
      return new HttpResponse(null, { status: 401 });
    }
    
    // Check if user has access to this job
    const hasAccess = 
      user.roles.includes('admin') || 
      user.allowedJobs.includes('*') || 
      user.allowedJobs.includes(jobId as string);
    
    if (!hasAccess) {
      return new HttpResponse(null, { status: 403 });
    }
    
    const job = jobs.find(j => j.id === jobId);
    
    if (job) {
      return HttpResponse.json(generateRunHistory(job.id));
    }
    
    return new HttpResponse(null, {
      status: 404,
      statusText: 'Not Found',
    });
  }),
  
  http.post('/api/jobs/:jobId/start', async ({ params, request }) => {
    await delay(500);
    
    const { jobId } = params;
    
    // Get user from cookies for access control
    const user = getUserFromCookies(request);
    
    if (!user) {
      return new HttpResponse(null, { status: 401 });
    }
    
    // Check if user has permission to start jobs
    const canStartJobs = 
      user.roles.includes('admin') || 
      user.roles.includes('operator');
    
    // Check if user has access to this job
    const hasAccess = 
      user.roles.includes('admin') || 
      user.allowedJobs.includes('*') || 
      user.allowedJobs.includes(jobId as string);
    
    if (!canStartJobs || !hasAccess) {
      return new HttpResponse(null, { status: 403 });
    }
    
    // Update job status
    jobs = jobs.map(job => {
      if (job.id === jobId) {
        return {
          ...job,
          status: 'running',
          lastRun: new Date().toISOString().replace('T', ' ').substring(0, 19),
          nextRun: null,
        };
      }
      return job;
    });
    
    return HttpResponse.json({
      success: true,
      message: 'Job started successfully',
    });
  }),
  
  http.post('/api/jobs/:jobId/stop', async ({ params, request }) => {
    await delay(500);
    
    const { jobId } = params;
    
    // Get user from cookies for access control
    const user = getUserFromCookies(request);
    
    if (!user) {
      return new HttpResponse(null, { status: 401 });
    }
    
    // Check if user has permission to stop jobs
    const canStopJobs = 
      user.roles.includes('admin') || 
      user.roles.includes('operator');
    
    // Check if user has access to this job
    const hasAccess = 
      user.roles.includes('admin') || 
      user.allowedJobs.includes('*') || 
      user.allowedJobs.includes(jobId as string);
    
    if (!canStopJobs || !hasAccess) {
      return new HttpResponse(null, { status: 403 });
    }
    
    // Update job status
    jobs = jobs.map(job => {
      if (job.id === jobId) {
        return {
          ...job,
          status: 'idle',
        };
      }
      return job;
    });
    
    return HttpResponse.json({
      success: true,
      message: 'Job stopped successfully',
    });
  }),
  
  http.delete('/api/jobs/:jobId', async ({ params, request }) => {
    await delay(700);
    
    const { jobId } = params;
    
    // Get user from cookies for access control
    const user = getUserFromCookies(request);
    
    if (!user) {
      return new HttpResponse(null, { status: 401 });
    }
    
    // Check if user has permission to delete jobs (admin only)
    const canDeleteJobs = user.roles.includes('admin');
    
    if (!canDeleteJobs) {
      return new HttpResponse(null, { status: 403 });
    }
    
    // Remove the job
    jobs = jobs.filter(job => job.id !== jobId);
    
    return HttpResponse.json({
      success: true,
      message: 'Job deleted successfully',
    });
  }),
  
  // Dashboard API
  http.get('/api/dashboard/stats', async ({ request }) => {
    await delay(300);
    
    // Get user from cookies for access control
    const user = getUserFromCookies(request);
    
    if (!user) {
      return new HttpResponse(null, { status: 401 });
    }
    
    // Calculate stats based on accessible jobs for this user
    let accessibleJobs = jobs;
    
    // If not admin (who can access all jobs), filter by allowed jobs
    if (!user.roles.includes('admin')) {
      accessibleJobs = jobs.filter(job => 
        user.allowedJobs.includes(job.id) || user.allowedJobs.includes('*')
      );
    }
    
    return HttpResponse.json({
      totalJobs: accessibleJobs.length,
      activeJobs: accessibleJobs.filter(job => job.status === 'running').length,
      failedJobs: accessibleJobs.filter(job => job.status === 'failed').length,
      successfulJobs: accessibleJobs.filter(job => job.status === 'success').length,
      pendingJobs: accessibleJobs.filter(job => job.status === 'idle').length,
    });
  }),
  
  http.get('/api/dashboard/recent-jobs', async ({ request }) => {
    await delay(300);
    
    // Get user from cookies for access control
    const user = getUserFromCookies(request);
    
    if (!user) {
      return new HttpResponse(null, { status: 401 });
    }
    
    // Filter jobs based on user permissions
    let accessibleJobs = jobs;
    
    // If not admin (who can access all jobs), filter by allowed jobs
    if (!user.roles.includes('admin')) {
      accessibleJobs = jobs.filter(job => 
        user.allowedJobs.includes(job.id) || user.allowedJobs.includes('*')
      );
    }
    
    return HttpResponse.json(
      accessibleJobs
        .slice()
        .sort((a, b) => {
          if (!a.lastRun) return 1;
          if (!b.lastRun) return -1;
          return new Date(b.lastRun).getTime() - new Date(a.lastRun).getTime();
        })
        .slice(0, 5)
        .map(job => ({
          jobName: job.name,
          status: job.status,
          lastRun: job.lastRun,
          duration: job.status === 'running' ? 'Running...' : '3m 45s',
        }))
    );
  }),
  
  // Include analytics handlers
  ...analyticsHandlers,
];
