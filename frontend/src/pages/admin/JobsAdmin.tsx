import React, { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Typography,
  Paper,
  Button,
  Snackbar,
  Alert,
  CircularProgress,
  Chip
} from '@mui/material';
import { DataGrid, GridColDef, GridRowModel, GridRowModes, GridRowModesModel } from '@mui/x-data-grid';
import { Add as AddIcon, Save as SaveIcon, Cancel as CancelIcon, Edit as EditIcon } from '@mui/icons-material';
import axios from 'axios';
import { API_BASE_URL } from '../../config/constants';

interface Job {
  id: string;
  name: string;
  description: string;
  jobType: string;
  status: string;
  schedule: string;
  parameters: Record<string, any>;
  createdAt: string;
  updatedAt: string;
  lastRunAt?: string;
  isEnabled: boolean;
}

const JobsAdmin: React.FC = () => {
  const [jobs, setJobs] = useState<Job[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [snackbar, setSnackbar] = useState<{ open: boolean; message: string; severity: 'success' | 'error' }>({
    open: false,
    message: '',
    severity: 'success',
  });
  const [rowModesModel, setRowModesModel] = useState<GridRowModesModel>({});

  // Fetch jobs from API
  const fetchJobs = useCallback(async () => {
    try {
      setLoading(true);
      const response = await axios.get(`${API_BASE_URL}/api/jobs/all`);
      setJobs(response.data);
      setError(null);
    } catch (err) {
      console.error('Error fetching jobs:', err);
      setError('Failed to load jobs. Please try again later.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchJobs();
  }, [fetchJobs]);

  // Handle editing row
  const handleEditClick = (id: string) => () => {
    setRowModesModel({ ...rowModesModel, [id]: { mode: GridRowModes.Edit } });
  };

  // Handle save row
  const handleSaveClick = (id: string) => () => {
    setRowModesModel({ ...rowModesModel, [id]: { mode: GridRowModes.View } });
  };

  // Handle cancel edit
  const handleCancelClick = (id: string) => () => {
    setRowModesModel({
      ...rowModesModel,
      [id]: { mode: GridRowModes.View, ignoreModifications: true },
    });
  };

  // Handle row edit stop
  const handleRowEditStop = () => {
    // Logic to run when edit is stopped
  };

  // Process row update - implement optimistic update
  const processRowUpdate = async (newRow: GridRowModel, oldRow: GridRowModel) => {
    // Clone the jobs array for optimistic update
    const updatedJobs = jobs.map(job => 
      job.id === newRow.id ? { ...job, ...newRow } : job
    );
    
    // Apply optimistic update
    setJobs(updatedJobs);
    
    try {
      // Send update to server
      await axios.put(`${API_BASE_URL}/api/jobs/${newRow.id}`, newRow);
      
      // Show success message
      setSnackbar({ open: true, message: 'Job updated successfully', severity: 'success' });
      return newRow;
    } catch (error) {
      // Revert optimistic update on error
      setJobs(jobs);
      setSnackbar({ open: true, message: 'Failed to update job', severity: 'error' });
      return oldRow;
    }
  };

  // Handle process row update error
  const handleProcessRowUpdateError = (error: Error) => {
    setSnackbar({ open: true, message: error.message, severity: 'error' });
  };

  // Handle add new job
  const handleAddJob = () => {
    const newId = `temp-${Date.now()}`;
    const newJob: Job = {
      id: newId,
      name: '',
      description: '',
      jobType: 'scheduled',
      status: 'idle',
      schedule: '0 0 * * *', // Daily at midnight
      parameters: {},
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isEnabled: false
    };
    
    setJobs(prevJobs => [newJob, ...prevJobs]);
    setRowModesModel(prevModel => ({
      ...prevModel,
      [newId]: { mode: GridRowModes.Edit, fieldToFocus: 'name' }
    }));
  };

  // Handle snackbar close
  const handleSnackbarClose = () => {
    setSnackbar({ ...snackbar, open: false });
  };

  // Get status color
  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'running':
        return 'primary';
      case 'completed':
        return 'success';
      case 'failed':
        return 'error';
      case 'idle':
        return 'default';
      case 'scheduled':
        return 'info';
      default:
        return 'default';
    }
  };

  // DataGrid columns definition
  const columns: GridColDef[] = [
    { field: 'name', headerName: 'Name', flex: 1, editable: true },
    { field: 'description', headerName: 'Description', flex: 2, editable: true },
    { 
      field: 'jobType', 
      headerName: 'Type', 
      flex: 0.7, 
      editable: true,
      type: 'singleSelect',
      valueOptions: ['scheduled', 'manual', 'webhook', 'event-triggered'],
    },
    { 
      field: 'status', 
      headerName: 'Status', 
      flex: 0.7, 
      editable: false,
      renderCell: (params) => (
        <Chip 
          label={params.value} 
          color={getStatusColor(params.value)} 
          size="small" 
          variant="outlined" 
        />
      )
    },
    { 
      field: 'schedule', 
      headerName: 'Schedule', 
      flex: 1, 
      editable: true,
    },
    { 
      field: 'isEnabled', 
      headerName: 'Enabled', 
      flex: 0.5, 
      editable: true,
      type: 'boolean' 
    },
    { 
      field: 'lastRunAt', 
      headerName: 'Last Run', 
      flex: 1, 
      editable: false,
      valueFormatter: (params) => {
        if (!params.value) return 'Never';
        return new Date(params.value).toLocaleString();
      }
    },
    {
      field: 'actions',
      type: 'actions',
      headerName: 'Actions',
      flex: 0.7,
      cellClassName: 'actions',
      getActions: ({ id }) => {
        const isInEditMode = rowModesModel[id]?.mode === GridRowModes.Edit;

        if (isInEditMode) {
          return [
            <Button 
              key="save" 
              variant="outlined" 
              color="primary" 
              size="small"
              startIcon={<SaveIcon />}
              onClick={handleSaveClick(id.toString())}
            >
              Save
            </Button>,
            <Button 
              key="cancel" 
              variant="outlined" 
              color="secondary" 
              size="small"
              startIcon={<CancelIcon />}
              onClick={handleCancelClick(id.toString())}
            >
              Cancel
            </Button>
          ];
        }

        return [
          <Button 
            key="edit" 
            variant="outlined" 
            color="primary" 
            size="small"
            startIcon={<EditIcon />}
            onClick={handleEditClick(id.toString())}
          >
            Edit
          </Button>
        ];
      }
    }
  ];

  return (
    <Box sx={{ height: '100%', width: '100%', padding: 2 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        Job Management
      </Typography>
      
      <Box sx={{ mb: 2 }}>
        <Button
          variant="contained"
          color="primary"
          startIcon={<AddIcon />}
          onClick={handleAddJob}
        >
          Add Job
        </Button>
      </Box>
      
      <Paper elevation={3} sx={{ height: 'calc(100vh - 200px)', width: '100%' }}>
        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
            <CircularProgress />
          </Box>
        ) : error ? (
          <Box sx={{ p: 2 }}>
            <Alert severity="error">{error}</Alert>
          </Box>
        ) : (
          <DataGrid
            rows={jobs}
            columns={columns}
            editMode="row"
            rowModesModel={rowModesModel}
            onRowModesModelChange={setRowModesModel}
            onRowEditStop={handleRowEditStop}
            processRowUpdate={processRowUpdate}
            onProcessRowUpdateError={handleProcessRowUpdateError}
            pageSizeOptions={[10, 25, 50]}
            initialState={{
              pagination: {
                paginationModel: { page: 0, pageSize: 10 },
              },
            }}
          />
        )}
      </Paper>
      
      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={handleSnackbarClose}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Alert
          onClose={handleSnackbarClose}
          severity={snackbar.severity}
          variant="filled"
          sx={{ width: '100%' }}
        >
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default JobsAdmin;