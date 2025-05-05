import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Grid,
  Typography,
  CircularProgress,
  Paper,
  Button,
  Divider,
} from '@mui/material';
import axios from 'axios';
import { useAuth } from '../auth';
import JobStatsCard from '../components/JobStatsCard';
import { pluginManager } from '../plugins';

interface DashboardStats {
  totalJobs: number;
  activeJobs: number;
  failedJobs: number;
  successfulJobs: number;
  pendingJobs: number;
}

interface JobStatus {
  jobName: string;
  status: string;
  lastRun: string;
  duration: string;
}

const Dashboard: React.FC = () => {
  const { user, hasRole } = useAuth();
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [recentJobs, setRecentJobs] = useState<JobStatus[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        setLoading(true);
        // API endpoints
        const [statsResponse, recentJobsResponse] = await Promise.all([
          axios.get('/api/dashboard/stats'),
          axios.get('/api/dashboard/recent-jobs'),
        ]);

        setStats(statsResponse.data);
        setRecentJobs(recentJobsResponse.data);
        setError(null);
      } catch (err) {
        console.error('Failed to fetch dashboard data', err);
        setError('Failed to load dashboard data. Please try again later.');
      } finally {
        setLoading(false);
      }
    };

    fetchDashboardData();
  }, []);

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="60vh">
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="60vh">
        <Typography color="error">{error}</Typography>
      </Box>
    );
  }

  // Fallback to mock data if needed (should be handled by MSW in development)
  const dashboardStats: DashboardStats = stats || {
    totalJobs: 45,
    activeJobs: 12,
    failedJobs: 3,
    successfulJobs: 28,
    pendingJobs: 2,
  };

  const dashboardRecentJobs: JobStatus[] = recentJobs.length > 0 ? recentJobs : [
    {
      jobName: 'Production Deploy',
      status: 'Success',
      lastRun: '2025-05-05 09:30:00',
      duration: '3m 45s',
    },
    {
      jobName: 'Database Backup',
      status: 'Success',
      lastRun: '2025-05-05 08:00:00',
      duration: '2m 15s',
    },
    {
      jobName: 'Test Environment Reset',
      status: 'Failed',
      lastRun: '2025-05-04 23:00:00',
      duration: '1m 30s',
    },
    {
      jobName: 'Integration Tests',
      status: 'Running',
      lastRun: '2025-05-05 10:15:00',
      duration: 'Running...',
    },
  ];

  // Function to get status color
  const getStatusColor = (status: string): 'success.main' | 'error.main' | 'primary.main' | 'text.primary' => {
    switch (status.toLowerCase()) {
      case 'success':
        return 'success.main';
      case 'failed':
        return 'error.main';
      case 'running':
        return 'primary.main';
      default:
        return 'text.primary';
    }
  };

  return (
    <div>
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" gutterBottom>
          Dashboard
        </Typography>
        <Typography variant="body1" color="textSecondary">
          Welcome back, {user?.username}! Here's an overview of your deployment jobs.
        </Typography>
      </Box>

      {/* Stats Cards */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={4} lg={2.4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Total Jobs
              </Typography>
              <Typography variant="h3">{dashboardStats.totalJobs}</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={4} lg={2.4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Active Jobs
              </Typography>
              <Typography variant="h3" color="primary">
                {dashboardStats.activeJobs}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={4} lg={2.4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Successful Jobs
              </Typography>
              <Typography variant="h3" color="success.main">
                {dashboardStats.successfulJobs}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={4} lg={2.4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Failed Jobs
              </Typography>
              <Typography variant="h3" color="error.main">
                {dashboardStats.failedJobs}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={4} lg={2.4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Pending Jobs
              </Typography>
              <Typography variant="h3" color="warning.main">
                {dashboardStats.pendingJobs}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Main Dashboard Content */}
      <Grid container spacing={4}>
        {/* Recent Jobs Section */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent sx={{ pb: 0 }}>
              <Typography variant="h5" gutterBottom>
                Recent Jobs
              </Typography>
            </CardContent>
            
            <Divider sx={{ my: 2 }} />
            
            <Box sx={{ px: 2, pb: 2 }}>
              {dashboardRecentJobs.map((job, index) => (
                <Box 
                  key={index}
                  sx={{ 
                    py: 1.5,
                    borderBottom: index < dashboardRecentJobs.length - 1 ? '1px solid' : 'none',
                    borderColor: 'divider',
                  }}
                >
                  <Grid container alignItems="center">
                    <Grid item xs={6}>
                      <Typography variant="body1" fontWeight="medium">
                        {job.jobName}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {job.lastRun}
                      </Typography>
                    </Grid>
                    <Grid item xs={3}>
                      <Typography
                        sx={{
                          color: getStatusColor(job.status),
                          fontWeight: 'medium',
                        }}
                      >
                        {job.status}
                      </Typography>
                    </Grid>
                    <Grid item xs={3} textAlign="right">
                      <Typography>{job.duration}</Typography>
                    </Grid>
                  </Grid>
                </Box>
              ))}
              
              <Box sx={{ mt: 2, textAlign: 'center' }}>
                <Button variant="outlined" size="small">
                  View All Jobs
                </Button>
              </Box>
            </Box>
          </Card>
        </Grid>
        
        {/* Job Statistics Card - Only shown to Admin and Operator roles */}
        {hasRole(['admin', 'operator']) && (
          <Grid item xs={12} md={6}>
            <JobStatsCard />
          </Grid>
        )}
      </Grid>
    </div>
  );
};

export default Dashboard;