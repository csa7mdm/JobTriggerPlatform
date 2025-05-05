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

interface Permission {
  id: string;
  name: string;
  description: string;
}

interface Role {
  id: string;
  name: string;
  description: string;
  permissions: string[];
}

const RolesAdmin: React.FC = () => {
  const [roles, setRoles] = useState<Role[]>([]);
  const [permissions, setPermissions] = useState<Permission[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [snackbar, setSnackbar] = useState<{ open: boolean; message: string; severity: 'success' | 'error' }>({
    open: false,
    message: '',
    severity: 'success',
  });
  const [rowModesModel, setRowModesModel] = useState<GridRowModesModel>({});

  // Fetch roles and permissions from API
  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      const [rolesResponse, permissionsResponse] = await Promise.all([
        axios.get(`${API_BASE_URL}/api/roles`),
        axios.get(`${API_BASE_URL}/api/permissions`)
      ]);
      setRoles(rolesResponse.data);
      setPermissions(permissionsResponse.data);
      setError(null);
    } catch (err) {
      console.error('Error fetching data:', err);
      setError('Failed to load roles and permissions. Please try again later.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

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
    // Clone the roles array for optimistic update
    const updatedRoles = roles.map(role => 
      role.id === newRow.id ? { ...role, ...newRow } : role
    );
    
    // Apply optimistic update
    setRoles(updatedRoles);
    
    try {
      // Send update to server
      await axios.put(`${API_BASE_URL}/api/roles/${newRow.id}`, newRow);
      
      // Show success message
      setSnackbar({ open: true, message: 'Role updated successfully', severity: 'success' });
      return newRow;
    } catch (error) {
      // Revert optimistic update on error
      setRoles(roles);
      setSnackbar({ open: true, message: 'Failed to update role', severity: 'error' });
      return oldRow;
    }
  };

  // Handle process row update error
  const handleProcessRowUpdateError = (error: Error) => {
    setSnackbar({ open: true, message: error.message, severity: 'error' });
  };

  // Handle add new role
  const handleAddRole = () => {
    const newId = `temp-${Date.now()}`;
    const newRole: Role = {
      id: newId,
      name: '',
      description: '',
      permissions: []
    };
    
    setRoles(prevRoles => [newRole, ...prevRoles]);
    setRowModesModel(prevModel => ({
      ...prevModel,
      [newId]: { mode: GridRowModes.Edit, fieldToFocus: 'name' }
    }));
  };

  // Handle snackbar close
  const handleSnackbarClose = () => {
    setSnackbar({ ...snackbar, open: false });
  };

  // Get permission name by id
  const getPermissionNameById = (id: string) => {
    const permission = permissions.find(p => p.id === id);
    return permission ? permission.name : id;
  };

  // DataGrid columns definition
  const columns: GridColDef[] = [
    { field: 'name', headerName: 'Name', flex: 1, editable: true },
    { field: 'description', headerName: 'Description', flex: 2, editable: true },
    { 
      field: 'permissions',
      headerName: 'Permissions',
      flex: 2,
      editable: true,
      renderCell: (params) => (
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
          {params.value.map((permissionId: string) => (
            <Chip 
              key={permissionId} 
              label={getPermissionNameById(permissionId)} 
              size="small" 
              color="primary" 
              variant="outlined" 
            />
          ))}
        </Box>
      )
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
    <Box sx={{ height: '100%', width: '100%', padding: 2 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        Role Management
      </Typography>
      
      <Box sx={{ mb: 2 }}>
        <Button
          variant="contained"
          color="primary"
          startIcon={<AddIcon />}
          onClick={handleAddRole}
        >
          Add Role
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
            rows={roles}
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

export default RolesAdmin;