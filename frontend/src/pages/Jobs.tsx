import React, { useState, useEffect } from 'react';
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Typography,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  PlayArrow as PlayArrowIcon,
  Stop as StopIcon,
  Delete as DeleteIcon,
  Info as InfoIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';

interface Job {
  id: string;
  name: string;
  description: string;
  status: 'idle' | 'running' | 'success' | 'failed';
  lastRun: string | null;
  nextRun: string | null;
  createdAt: string;
}

const Jobs: React.FC = () => {
  const navigate = useNavigate();
  const [jobs, setJobs] = useState<Job[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [openDeleteDialog, setOpenDeleteDialog] = useState(false);
  const [jobToDelete, setJobToDelete] = useState<Job | null>(null);
  const [deleteLoading, setDeleteLoading] = useState(false);

  const fetchJobs = async () => {
    try {
      setLoading(true);
      // Replace with your actual API endpoint
      const response = await axios.get('/api/jobs');
      setJobs(response.data);
      setError(null);
    } catch (err) {
      console.error('Failed to fetch jobs', err);
      setError('Failed to load jobs. Please try again later.');
      
      // Mock data for development
      setJobs([
        {
          id: '1',
          name: 'Production Deploy',
          description: 'Deploy to production environment',
          status: 'success',
          lastRun: '2025-05-05 09:30:00',
          nextRun: '2025-05-06 09:30:00',
          createdAt: '2025-01-01 00:00:00',
        },
        {
          id: '2',
          name: 'Database Backup',
          description: 'Backup all production databases',
          status: 'success',
          lastRun: '2025-05-05 08:00:00',
          nextRun: '2025-05-06 08:00:00',
          createdAt: '2025-01-02 00:00:00',
        },
        {
          id: '3',
          name: 'Test Environment Reset',
          description: 'Reset test environment to clean state',
          status: 'failed',
          lastRun: '2025-05-04 23:00:00',
          nextRun: '2025-05-05 23:00:00',
          createdAt: '2025-01-03 00:00:00',
        },
        {
          id: '4',
          name: 'Integration Tests',
          description: 'Run all integration tests',
          status: 'running',
          lastRun: '2025-05-05 10:15:00',
          nextRun: null,
          createdAt: '2025-01-04 00:00:00',
        },
        {
          id: '5',
          name: 'Code Analysis',
          description: 'Run static code analysis',
          status: 'idle',
          lastRun: '2025-05-03 12:00:00',
          nextRun: '2025-05-06 12:00:00',
          createdAt: '2025-01-05 00:00:00',
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchJobs();
  }, []);

  const handleChangePage = (_event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const handleSearch = (event: React.ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(event.target.value);
    setPage(0);
  };

  const handleRefresh = () => {
    fetchJobs();
  };

  const handleStartJob = async (jobId: string) => {
    try {
      // Replace with your actual API endpoint
      await axios.post(`/api/jobs/${jobId}/start`);
      fetchJobs();
    } catch (err) {
      console.error('Failed to start job', err);
      // Update the UI optimistically
      setJobs(
        jobs.map((job) =>
          job.id === jobId ? { ...job, status: 'running' as const } : job
        )
      );
    }
  };

  const handleStopJob = async (jobId: string) => {
    try {
      // Replace with your actual API endpoint
      await axios.post(`/api/jobs/${jobId}/stop`);
      fetchJobs();
    } catch (err) {
      console.error('Failed to stop job', err);
      // Update the UI optimistically
      setJobs(
        jobs.map((job) =>
          job.id === jobId ? { ...job, status: 'idle' as const } : job
        )
      );
    }
  };

  const handleDeleteClick = (job: Job) => {
    setJobToDelete(job);
    setOpenDeleteDialog(true);
  };

  const handleDeleteConfirm = async () => {
    if (!jobToDelete) return;

    try {
      setDeleteLoading(true);
      // Replace with your actual API endpoint
      await axios.delete(`/api/jobs/${jobToDelete.id}`);
      setJobs(jobs.filter((job) => job.id !== jobToDelete.id));
      setOpenDeleteDialog(false);
      setJobToDelete(null);
    } catch (err) {
      console.error('Failed to delete job', err);
    } finally {
      setDeleteLoading(false);
    }
  };

  const handleDeleteCancel = () => {
    setOpenDeleteDialog(false);
    setJobToDelete(null);
  };

  const handleJobClick = (jobId: string) => {
    navigate(`/jobs/${jobId}`);
  };

  const filteredJobs = jobs.filter((job) =>
    job.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    job.description.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const paginatedJobs = filteredJobs.slice(
    page * rowsPerPage,
    page * rowsPerPage + rowsPerPage
  );

  const getStatusChip = (status: Job['status']) => {
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

  return (
    <div>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" sx={{ flexGrow: 1 }}>
          Jobs
        </Typography>
        <Button
          variant="contained"
          startIcon={<RefreshIcon />}
          onClick={handleRefresh}
          sx={{ ml: 2 }}
        >
          Refresh
        </Button>
      </Box>
      
      <TextField
        label="Search Jobs"
        variant="outlined"
        fullWidth
        margin="normal"
        value={searchTerm}
        onChange={handleSearch}
        sx={{ mb: 3 }}
      />
      
      {loading ? (
        <Box display="flex" justifyContent="center" alignItems="center" minHeight="50vh">
          <CircularProgress />
        </Box>
      ) : error ? (
        <Typography color="error" align="center">
          {error}
        </Typography>
      ) : (
        <>
          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Name</TableCell>
                  <TableCell>Description</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Last Run</TableCell>
                  <TableCell>Next Run</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {paginatedJobs.map((job) => (
                  <TableRow key={job.id} hover>
                    <TableCell 
                      onClick={() => handleJobClick(job.id)}
                      sx={{ cursor: 'pointer' }}
                    >
                      <Typography fontWeight="medium">{job.name}</Typography>
                    </TableCell>
                    <TableCell>{job.description}</TableCell>
                    <TableCell>{getStatusChip(job.status)}</TableCell>
                    <TableCell>{job.lastRun || 'Never'}</TableCell>
                    <TableCell>{job.nextRun || 'Not scheduled'}</TableCell>
                    <TableCell align="right">
                      <Box sx={{ '& > button': { ml: 1 } }}>
                        <Tooltip title="View Details">
                          <IconButton 
                            color="primary"
                            onClick={() => handleJobClick(job.id)}
                          >
                            <InfoIcon />
                          </IconButton>
                        </Tooltip>
                        
                        {job.status === 'running' ? (
                          <Tooltip title="Stop Job">
                            <IconButton 
                              color="error"
                              onClick={() => handleStopJob(job.id)}
                            >
                              <StopIcon />
                            </IconButton>
                          </Tooltip>
                        ) : (
                          <Tooltip title="Start Job">
                            <IconButton 
                              color="success"
                              onClick={() => handleStartJob(job.id)}
                            >
                              <PlayArrowIcon />
                            </IconButton>
                          </Tooltip>
                        )}
                        
                        <Tooltip title="Delete Job">
                          <IconButton 
                            color="error"
                            onClick={() => handleDeleteClick(job)}
                          >
                            <DeleteIcon />
                          </IconButton>
                        </Tooltip>
                      </Box>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          
          <TablePagination
            rowsPerPageOptions={[5, 10, 25]}
            component="div"
            count={filteredJobs.length}
            rowsPerPage={rowsPerPage}
            page={page}
            onPageChange={handleChangePage}
            onRowsPerPageChange={handleChangeRowsPerPage}
          />
        </>
      )}
      
      {/* Delete Confirmation Dialog */}
      <Dialog
        open={openDeleteDialog}
        onClose={handleDeleteCancel}
      >
        <DialogTitle>Confirm Delete</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete job "{jobToDelete?.name}"? This action cannot be undone.
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

export default Jobs;