import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Navigate } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Divider,
  Grid,
  Paper,
  Tab,
  Tabs,
  Typography,
  IconButton,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  PlayArrow as PlayArrowIcon,
  Stop as StopIcon,
  Delete as DeleteIcon,
  Schedule as ScheduleIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import axios from 'axios';
import { useAuth } from '../auth';
import { SafeHtml } from '../components/common';
import { sanitizePlainText } from '../utils/sanitization/htmlSanitizer';
import './JobDetail.css';

interface JobDetails {
  id: string;
  name: string;
  description: string;
  status: 'idle' | 'running' | 'success' | 'failed';
  lastRun: string | null;
  nextRun: string | null;
  createdAt: string;
  createdBy: string;
  command: string;
  timeout: number;
  maxRetries: number;
  retryDelay: number;
  environment: string;
  tags: string[];
}

interface LogEntry {
  id: string;
  timestamp: string;
  level: 'info' | 'warning' | 'error';
  message: string;
}

interface RunHistory {
  id: string;
  startTime: string;
  endTime: string | null;
  status: 'running' | 'success' | 'failed';
  duration: string | null;
  triggeredBy: string;
}

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = (props) => {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`job-tabpanel-${index}`}
      aria-labelledby={`job-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ py: 3 }}>{children}</Box>}
    </div>
  );
};

const JobDetail: React.FC = () => {
  const { jobName } = useParams<{ jobName: string }>();
  const navigate = useNavigate();
  const { user, canAccessJob, hasRole } = useAuth();
  const [tabValue, setTabValue] = useState(0);
  const [jobDetails, setJobDetails] = useState<JobDetails | null>(null);
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [runHistory, setRunHistory] = useState<RunHistory[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [openDeleteDialog, setOpenDeleteDialog] = useState(false);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const [permissionError, setPermissionError] = useState(false);

  const fetchJobData = async () => {
    try {
      setLoading(true);
      // Fetch job details first to get the ID
      const detailsResponse = await axios.get(`/api/jobs/${jobName}`);
      const jobData = detailsResponse.data;
      
      // Check if user has permission for this specific job
      if (!canAccessJob(jobData.id) && !hasRole('admin')) {
        setPermissionError(true);
        return;
      }

      setJobDetails(jobData);
      
      // Now fetch logs and history
      const [logsResponse, historyResponse] = await Promise.all([
        axios.get(`/api/jobs/${jobName}/logs`),
        axios.get(`/api/jobs/${jobName}/history`),
      ]);

      setLogs(logsResponse.data);
      setRunHistory(historyResponse.data);
      setError(null);
    } catch (err) {
      console.error('Failed to fetch job data', err);
      setError('Failed to load job details. Please try again later.');
      
      // Provide mock data for development
      if (process.env.NODE_ENV === 'development') {
        setJobDetails({
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
        });
        
        setLogs([
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
          },
        ]);
        
        setRunHistory([
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
          },
        ]);
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (jobName) {
      fetchJobData();
    }
  }, [jobName]);

  // If permission error, redirect to unauthorized page
  if (permissionError) {
    return <Navigate to="/unauthorized" />;
  }

  // Check user's abilities based on roles
  const canStart = hasRole(['admin', 'operator']);
  const canStop = hasRole(['admin', 'operator']);
  const canDelete = hasRole('admin');

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  const handleBack = () => {
    navigate('/jobs');
  };

  const handleRefresh = () => {
    fetchJobData();
  };

  const handleStartJob = async () => {
    if (!jobDetails || !canStart) return;

    try {
      // Call API to start the job
      await axios.post(`/api/jobs/${jobDetails.id}/start`);
      fetchJobData();
    } catch (err) {
      console.error('Failed to start job', err);
      // Update the UI optimistically
      setJobDetails(prev => prev ? { ...prev, status: 'running' } : null);
    }
  };

  const handleStopJob = async () => {
    if (!jobDetails || !canStop) return;

    try {
      // Call API to stop the job
      await axios.post(`/api/jobs/${jobDetails.id}/stop`);
      fetchJobData();
    } catch (err) {
      console.error('Failed to stop job', err);
      // Update the UI optimistically
      setJobDetails(prev => prev ? { ...prev, status: 'idle' } : null);
    }
  };

  const handleDeleteClick = () => {
    if (!canDelete) return;
    setOpenDeleteDialog(true);
  };

  const handleDeleteConfirm = async () => {
    if (!jobDetails || !canDelete) return;

    try {
      setDeleteLoading(true);
      // Call API to delete the job
      await axios.delete(`/api/jobs/${jobDetails.id}`);
      navigate('/jobs');
    } catch (err) {
      console.error('Failed to delete job', err);
    } finally {
      setDeleteLoading(false);
    }
  };

  const handleDeleteCancel = () => {
    setOpenDeleteDialog(false);
  };

  const getStatusChip = (status: string) => {
    let color: 'success' | 'error' | 'primary' | 'default';
    
    switch (status) {
      case 'success':
        color = 'success';
        break;
      case 'failed':
        color = 'error';
        break;
      case 'running':
        color = 'primary';
        break;
      default:
        color = 'default';
    }
    
    return (
      <Chip
        label={status.charAt(0).toUpperCase() + status.slice(1)}
        color={color}
        size="small"
      />
    );
  };

  const getLogLevelChip = (level: string) => {
    let color: 'success' | 'error' | 'warning' | 'default';
    
    switch (level) {
      case 'info':
        color = 'success';
        break;
      case 'error':
        color = 'error';
        break;
      case 'warning':
        color = 'warning';
        break;
      default:
        color = 'default';
    }
    
    return (
      <Chip
        label={level.charAt(0).toUpperCase() + level.slice(1)}
        color={color}
        size="small"
        sx={{ minWidth: 70 }}
      />
    );
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="60vh">
        <CircularProgress />
      </Box>
    );
  }

  if (error || !jobDetails) {
    return (
      <Box sx={{ mb: 2 }}>
        <Button 
          startIcon={<ArrowBackIcon />} 
          onClick={handleBack}
          sx={{ mb: 2 }}
        >
          Back to Jobs
        </Button>
        <Typography color="error">{error}</Typography>
      </Box>
    );
  }

  return (
    <div>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <Button 
          startIcon={<ArrowBackIcon />} 
          onClick={handleBack}
          sx={{ mr: 2 }}
        >
          Back
        </Button>
        <Typography variant="h4" sx={{ flexGrow: 1 }}>
          {jobDetails.name}
        </Typography>
        <Box sx={{ '& > button': { ml: 1 } }}>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={handleRefresh}
          >
            Refresh
          </Button>
          
          {jobDetails.status === 'running' ? (
            <Button 
              variant="contained" 
              color="error"
              startIcon={<StopIcon />}
              onClick={handleStopJob}
              disabled={!canStop}
            >
              Stop
            </Button>
          ) : (
            <Button 
              variant="contained" 
              color="primary"
              startIcon={<PlayArrowIcon />}
              onClick={handleStartJob}
              disabled={!canStart}
            >
              Start
            </Button>
          )}
          
          <Button 
            variant="outlined" 
            color="error"
            startIcon={<DeleteIcon />}
            onClick={handleDeleteClick}
            disabled={!canDelete}
          >
            Delete
          </Button>
        </Box>
      </Box>
      
      {/* Use SafeHtml component for job description */}
      <SafeHtml 
        html={jobDetails.description} 
        restrictedMode={true}
        className="job-description"
      />
      
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Status
              </Typography>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                {getStatusChip(jobDetails.status)}
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Last Run
              </Typography>
              <Typography>
                {jobDetails.lastRun || 'Never'}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Next Run
              </Typography>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <ScheduleIcon sx={{ mr: 1, color: 'text.secondary' }} />
                <Typography>
                  {jobDetails.nextRun || 'Not scheduled'}
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
      
      <Paper sx={{ width: '100%', mb: 4 }}>
        <Tabs
          value={tabValue}
          onChange={handleTabChange}
          indicatorColor="primary"
          textColor="primary"
        >
          <Tab label="Details" id="job-tab-0" aria-controls="job-tabpanel-0" />
          <Tab label="Logs" id="job-tab-1" aria-controls="job-tabpanel-1" />
          <Tab label="Run History" id="job-tab-2" aria-controls="job-tabpanel-2" />
        </Tabs>
        
        <TabPanel value={tabValue} index={0}>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Typography variant="h6" gutterBottom>
                Basic Information
              </Typography>
              <Box sx={{ '& > div': { mb: 2 } }}>
                <Box>
                <Typography variant="subtitle2">Command</Typography>
                <Paper sx={{ p: 2, backgroundColor: 'grey.100' }}>
                <Typography variant="body2" component="pre" sx={{ fontFamily: 'monospace', whiteSpace: 'pre-wrap' }}>
                {sanitizePlainText(jobDetails.command)}
                </Typography>
                </Paper>
                </Box>
                <Box>
                  <Typography variant="subtitle2">Created By</Typography>
                  <Typography>{jobDetails.createdBy}</Typography>
                </Box>
                <Box>
                  <Typography variant="subtitle2">Created At</Typography>
                  <Typography>{jobDetails.createdAt}</Typography>
                </Box>
                <Box>
                  <Typography variant="subtitle2">Environment</Typography>
                  <Typography>{jobDetails.environment}</Typography>
                </Box>
                <Box>
                  <Typography variant="subtitle2">Tags</Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                    {jobDetails.tags.map((tag, index) => (
                      <Chip key={index} label={tag} size="small" />
                    ))}
                  </Box>
                </Box>
              </Box>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="h6" gutterBottom>
                Execution Settings
              </Typography>
              <Box sx={{ '& > div': { mb: 2 } }}>
                <Box>
                  <Typography variant="subtitle2">Timeout</Typography>
                  <Typography>{jobDetails.timeout} seconds</Typography>
                </Box>
                <Box>
                  <Typography variant="subtitle2">Max Retries</Typography>
                  <Typography>{jobDetails.maxRetries}</Typography>
                </Box>
                <Box>
                  <Typography variant="subtitle2">Retry Delay</Typography>
                  <Typography>{jobDetails.retryDelay} seconds</Typography>
                </Box>
                
                {/* Display user permissions for this job */}
                <Box sx={{ mt: 4, pt: 3, borderTop: '1px solid', borderColor: 'divider' }}>
                  <Typography variant="subtitle2">Your Permissions</Typography>
                  <Box sx={{ mt: 1 }}>
                    <Typography variant="body2">
                      View: <Chip 
                        label="Yes" 
                        color="success" 
                        size="small" 
                        sx={{ ml: 1 }} 
                      />
                    </Typography>
                    <Typography variant="body2" sx={{ mt: 1 }}>
                      Start/Stop: <Chip 
                        label={canStart ? "Yes" : "No"} 
                        color={canStart ? "success" : "error"} 
                        size="small" 
                        sx={{ ml: 1 }} 
                      />
                    </Typography>
                    <Typography variant="body2" sx={{ mt: 1 }}>
                      Delete: <Chip 
                        label={canDelete ? "Yes" : "No"} 
                        color={canDelete ? "success" : "error"} 
                        size="small" 
                        sx={{ ml: 1 }} 
                      />
                    </Typography>
                  </Box>
                </Box>
              </Box>
            </Grid>
          </Grid>
        </TabPanel>
        
        <TabPanel value={tabValue} index={1}>
          <Typography variant="h6" gutterBottom>
            Recent Logs
          </Typography>
          <Paper elevation={0} sx={{ overflow: 'hidden', border: '1px solid', borderColor: 'divider' }}>
            <Box sx={{ maxHeight: 400, overflowY: 'auto', p: 2 }}>
              {logs.length === 0 ? (
                <Typography color="text.secondary">No logs available</Typography>
              ) : (
                logs.map((log, index) => (
                  <Box key={log.id} sx={{ mb: 1.5 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 0.5 }}>
                      <Typography variant="caption" color="text.secondary" sx={{ mr: 2 }}>
                        {log.timestamp}
                      </Typography>
                      {getLogLevelChip(log.level)}
                    </Box>
                    <Typography variant="body2">
                      {sanitizePlainText(log.message)}
                    </Typography>
                    {index < logs.length - 1 && <Divider sx={{ mt: 1.5 }} />}
                  </Box>
                ))
              )}
            </Box>
          </Paper>
        </TabPanel>
        
        <TabPanel value={tabValue} index={2}>
          <Typography variant="h6" gutterBottom>
            Run History
          </Typography>
          <Paper elevation={0} sx={{ overflow: 'hidden', border: '1px solid', borderColor: 'divider' }}>
            <Box>
              {runHistory.length === 0 ? (
                <Typography color="text.secondary" sx={{ p: 2 }}>No run history available</Typography>
              ) : (
                runHistory.map((run, index) => (
                  <Box key={run.id} sx={{ p: 2 }}>
                    <Grid container spacing={2}>
                      <Grid item xs={12} sm={4}>
                        <Typography variant="subtitle2">Start Time</Typography>
                        <Typography variant="body2">{run.startTime}</Typography>
                      </Grid>
                      <Grid item xs={12} sm={4}>
                        <Typography variant="subtitle2">End Time</Typography>
                        <Typography variant="body2">{run.endTime || 'Running...'}</Typography>
                      </Grid>
                      <Grid item xs={12} sm={4}>
                        <Typography variant="subtitle2">Status</Typography>
                        <Box>{getStatusChip(run.status)}</Box>
                      </Grid>
                      <Grid item xs={12} sm={4}>
                        <Typography variant="subtitle2">Duration</Typography>
                        <Typography variant="body2">{run.duration || 'Running...'}</Typography>
                      </Grid>
                      <Grid item xs={12} sm={8}>
                        <Typography variant="subtitle2">Triggered By</Typography>
                        <Typography variant="body2">{run.triggeredBy}</Typography>
                      </Grid>
                    </Grid>
                    {index < runHistory.length - 1 && <Divider sx={{ mt: 2 }} />}
                  </Box>
                ))
              )}
            </Box>
          </Paper>
        </TabPanel>
      </Paper>
      
      {/* Delete Confirmation Dialog */}
      <Dialog
        open={openDeleteDialog}
        onClose={handleDeleteCancel}
      >
        <DialogTitle>Confirm Delete</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete job "{jobDetails.name}"? This action cannot be undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleDeleteCancel} disabled={deleteLoading}>
            Cancel
          </Button>
          <Button 
            onClick={handleDeleteConfirm} 
            color="error" 
            disabled={deleteLoading}
            startIcon={deleteLoading ? <CircularProgress size={20} /> : null}
          >
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </div>
  );
};

export default JobDetail;