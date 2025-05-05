import React, { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Typography,
  Paper,
  Button,
  Snackbar,
  Alert,
  CircularProgress
} from '@mui/material';
import { DataGrid, GridColDef, GridRowModel, GridRowModes, GridRowModesModel } from '@mui/x-data-grid';
import { Add as AddIcon, Save as SaveIcon, Cancel as CancelIcon, Edit as EditIcon } from '@mui/icons-material';
import axios from 'axios';
import { API_BASE_URL } from '../../config/constants';
import { CspProvider } from '../../components/security';

interface User {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  roleIds: string[];
}

const UsersAdmin: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [snackbar, setSnackbar] = useState<{ open: boolean; message: string; severity: 'success' | 'error' }>({
    open: false,
    message: '',
    severity: 'success',
  });
  const [rowModesModel, setRowModesModel] = useState<GridRowModesModel>({});

  // Fetch users from API
  const fetchUsers = useCallback(async () => {
    try {
      setLoading(true);
      const response = await axios.get(`${API_BASE_URL}/api/users`);
      setUsers(response.data);
      setError(null);
    } catch (err) {
      console.error('Error fetching users:', err);
      setError('Failed to load users. Please try again later.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

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
    // Clone the users array for optimistic update
    const updatedUsers = users.map(user => 
      user.id === newRow.id ? { ...user, ...newRow } : user
    );
    
    // Apply optimistic update
    setUsers(updatedUsers);
    
    try {
      // Send update to server
      await axios.put(`${API_BASE_URL}/api/users/${newRow.id}`, newRow);
      
      // Show success message
      setSnackbar({ open: true, message: 'User updated successfully', severity: 'success' });
      return newRow;
    } catch (error) {
      // Revert optimistic update on error
      setUsers(users);
      setSnackbar({ open: true, message: 'Failed to update user', severity: 'error' });
      return oldRow;
    }
  };

  // Handle process row update error
  const handleProcessRowUpdateError = (error: Error) => {
    setSnackbar({ open: true, message: error.message, severity: 'error' });
  };

  // Handle add new user
  const handleAddUser = () => {
    const newId = `temp-${Date.now()}`;
    const newUser: User = {
      id: newId,
      username: '',
      email: '',
      firstName: '',
      lastName: '',
      isActive: true,
      roleIds: []
    };
    
    setUsers(prevUsers => [newUser, ...prevUsers]);
    setRowModesModel(prevModel => ({
      ...prevModel,
      [newId]: { mode: GridRowModes.Edit, fieldToFocus: 'username' }
    }));
  };

  // Handle snackbar close
  const handleSnackbarClose = () => {
    setSnackbar({ ...snackbar, open: false });
  };

  // DataGrid columns definition
  const columns: GridColDef[] = [
    { field: 'username', headerName: 'Username', flex: 1, editable: true },
    { field: 'email', headerName: 'Email', flex: 1, editable: true },
    { field: 'firstName', headerName: 'First Name', flex: 1, editable: true },
    { field: 'lastName', headerName: 'Last Name', flex: 1, editable: true },
    { 
      field: 'isActive', 
      headerName: 'Status', 
      flex: 0.5, 
      editable: true,
      type: 'boolean' 
    },
    {
      field: 'actions',
      type: 'actions',
      headerName: 'Actions',
      flex: 0.5,
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
    <CspProvider
      allowForms={true}
      additionalConnectSrc={[
        'https://auth-service.example.com', // For user verification API
        'https://analytics.example.com'    // For admin analytics
      ]}
    >
      <Box sx={{ height: '100%', width: '100%', padding: 2 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        User Management
      </Typography>
      
      <Box sx={{ mb: 2 }}>
        <Button
          variant="contained"
          color="primary"
          startIcon={<AddIcon />}
          onClick={handleAddUser}
        >
          Add User
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
            rows={users}
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
    </CspProvider>
  );
};

export default UsersAdmin;