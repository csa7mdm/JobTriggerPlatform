import React, { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Typography,
  Paper,
  Button,
  Snackbar,
  Alert,
  CircularProgress,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  IconButton,
  Tooltip,
  Modal,
} from '@mui/material';
import {
  DataGrid,
  GridColDef,
  GridRowModel,
  GridRowParams,
  GridRenderCellParams,
} from '@mui/x-data-grid';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  VpnKey as ApiKeyIcon,
  ContentCopy as CopyIcon,
  Close as CloseIcon,
} from '@mui/icons-material';
import { Tenant, CreateTenantDto, UpdateTenantDto } from '../../types/tenant';
import { tenantApi } from '../../api/tenantApi';
import SingleTenantForm from '../../components/tenant/SingleTenantForm';
import { CspProvider } from '../../components/security';

const TenantsAdmin: React.FC = () => {
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [snackbar, setSnackbar] = useState<{
    open: boolean;
    message: string;
    severity: 'success' | 'error' | 'info';
  }>({
    open: false,
    message: '',
    severity: 'success',
  });

  // Form modal state
  const [formOpen, setFormOpen] = useState(false);
  const [currentTenant, setCurrentTenant] = useState<Tenant | undefined>(undefined);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  // Delete confirmation dialog state
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [tenantToDelete, setTenantToDelete] = useState<Tenant | null>(null);

  // API key dialog state
  const [apiKeyDialogOpen, setApiKeyDialogOpen] = useState(false);
  const [currentApiKey, setCurrentApiKey] = useState<string | null>(null);
  const [apiKeyLoading, setApiKeyLoading] = useState(false);

  // Fetch tenants from API
  const fetchTenants = useCallback(async () => {
    try {
      setLoading(true);
      const response = await tenantApi.getAll();
      setTenants(response.data);
      setError(null);
    } catch (err) {
      console.error('Error fetching tenants:', err);
      setError('Failed to load tenants. Please try again later.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchTenants();
  }, [fetchTenants]);

  // Handle form open for edit
  const handleEditTenant = (tenant: Tenant) => {
    setCurrentTenant(tenant);
    setFormOpen(true);
  };

  // Handle form open for create
  const handleAddTenant = () => {
    setCurrentTenant(undefined);
    setFormOpen(true);
  };

  // Handle form close
  const handleFormClose = () => {
    setFormOpen(false);
    setCurrentTenant(undefined);
    setSubmitError(null);
  };

  // Handle form submit
  const handleFormSubmit = async (data: CreateTenantDto | UpdateTenantDto) => {
    setIsSubmitting(true);
    setSubmitError(null);
    
    try {
      if (currentTenant) {
        // Update existing tenant
        await tenantApi.update(currentTenant.id, data as UpdateTenantDto);
        // Apply optimistic update
        setTenants(prevTenants =>
          prevTenants.map(tenant =>
            tenant.id === currentTenant.id
              ? { ...tenant, ...data, updatedAt: new Date().toISOString() }
              : tenant
          )
        );
        setSnackbar({
          open: true,
          message: `Tenant "${data.name || currentTenant.name}" updated successfully`,
          severity: 'success',
        });
      } else {
        // Create new tenant
        const response = await tenantApi.create(data as CreateTenantDto);
        // Add new tenant to the list
        setTenants(prevTenants => [response.data, ...prevTenants]);
        setSnackbar({
          open: true,
          message: `Tenant "${data.name}" created successfully`,
          severity: 'success',
        });
      }
      handleFormClose();
      // Refresh data to ensure consistency
      fetchTenants();
    } catch (err: any) {
      console.error('Error saving tenant:', err);
      setSubmitError(
        err.response?.data?.message ||
          'Failed to save tenant. Please check your input and try again.'
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  // Handle delete confirmation open
  const handleDeleteConfirmation = (tenant: Tenant) => {
    setTenantToDelete(tenant);
    setDeleteDialogOpen(true);
  };

  // Handle delete tenant
  const handleDeleteTenant = async () => {
    if (!tenantToDelete) return;
    
    try {
      await tenantApi.delete(tenantToDelete.id);
      // Apply optimistic update
      setTenants(prevTenants =>
        prevTenants.filter(tenant => tenant.id !== tenantToDelete.id)
      );
      setSnackbar({
        open: true,
        message: `Tenant "${tenantToDelete.name}" deleted successfully`,
        severity: 'success',
      });
    } catch (err) {
      console.error('Error deleting tenant:', err);
      setSnackbar({
        open: true,
        message: 'Failed to delete tenant. Please try again later.',
        severity: 'error',
      });
    } finally {
      setDeleteDialogOpen(false);
      setTenantToDelete(null);
    }
  };

  // Handle API key generation
  const handleGenerateApiKey = async (tenant: Tenant) => {
    setApiKeyLoading(true);
    try {
      const response = await tenantApi.generateApiKey(tenant.id);
      setCurrentApiKey(response.data.apiKey);
      setApiKeyDialogOpen(true);
      
      // Update the tenant in the list to indicate it has an API key
      setTenants(prevTenants =>
        prevTenants.map(t =>
          t.id === tenant.id
            ? { ...t, apiKey: 'HIDDEN_API_KEY' }
            : t
        )
      );
    } catch (err) {
      console.error('Error generating API key:', err);
      setSnackbar({
        open: true,
        message: 'Failed to generate API key. Please try again later.',
        severity: 'error',
      });
    } finally {
      setApiKeyLoading(false);
    }
  };

  // Handle copy API key to clipboard
  const handleCopyApiKey = () => {
    if (currentApiKey) {
      navigator.clipboard.writeText(currentApiKey);
      setSnackbar({
        open: true,
        message: 'API key copied to clipboard',
        severity: 'info',
      });
    }
  };

  // Handle snackbar close
  const handleSnackbarClose = () => {
    setSnackbar({ ...snackbar, open: false });
  };

  // DataGrid columns definition
  const columns: GridColDef[] = [
    { field: 'name', headerName: 'Tenant Name', flex: 1 },
    { field: 'description', headerName: 'Description', flex: 1.5 },
    { 
      field: 'isActive', 
      headerName: 'Status', 
      flex: 0.5,
      renderCell: (params: GridRenderCellParams<Tenant, boolean>) => (
        <Chip 
          label={params.value ? 'Active' : 'Inactive'} 
          color={params.value ? 'success' : 'default'} 
          size="small" 
          variant="outlined" 
        />
      )
    },
    { field: 'contactName', headerName: 'Contact Name', flex: 1 },
    { field: 'contactEmail', headerName: 'Contact Email', flex: 1 },
    { 
      field: 'allowedJobs', 
      headerName: 'Job Access', 
      flex: 0.5,
      renderCell: (params: GridRenderCellParams<Tenant, string[]>) => (
        <Chip 
          label={`${params.value?.length || 0} Jobs`} 
          color="primary" 
          size="small" 
          variant="outlined" 
        />
      )
    },
    {
      field: 'actions',
      headerName: 'Actions',
      flex: 1,
      sortable: false,
      renderCell: (params: GridRenderCellParams<Tenant>) => (
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Tooltip title="Edit">
            <IconButton
              onClick={() => handleEditTenant(params.row)}
              color="primary"
              size="small"
            >
              <EditIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title="Delete">
            <IconButton
              onClick={() => handleDeleteConfirmation(params.row)}
              color="error"
              size="small"
            >
              <DeleteIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title="Generate API Key">
            <IconButton
              onClick={() => handleGenerateApiKey(params.row)}
              color="secondary"
              size="small"
              disabled={apiKeyLoading}
            >
              <ApiKeyIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        </Box>
      )
    }
  ];

  return (
    <CspProvider
      allowForms={true}
      additionalConnectSrc={[
        'https://auth-service.example.com',
        'https://analytics.example.com'
      ]}
    >
      <Box sx={{ height: '100%', width: '100%', padding: 2 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          Tenant Management
        </Typography>
        
        <Box sx={{ mb: 2 }}>
          <Button
            variant="contained"
            color="primary"
            startIcon={<AddIcon />}
            onClick={handleAddTenant}
          >
            Add Tenant
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
              rows={tenants}
              columns={columns}
              pageSizeOptions={[10, 25, 50]}
              initialState={{
                pagination: {
                  paginationModel: { page: 0, pageSize: 10 },
                },
              }}
              getRowId={(row) => row.id}
              localeText={{ noRowsLabel: 'No tenants found' }}
            />
          )}
        </Paper>
        
        {/* Tenant Form Modal */}
        <Modal
          open={formOpen}
          onClose={handleFormClose}
          aria-labelledby={currentTenant ? "edit-tenant-modal" : "add-tenant-modal"}
        >
          <Box sx={{ 
            position: 'absolute', 
            top: '50%', 
            left: '50%', 
            transform: 'translate(-50%, -50%)', 
            width: '90%', 
            maxWidth: 900, 
            maxHeight: '90vh',
            overflow: 'auto',
            bgcolor: 'background.paper', 
            boxShadow: 24, 
            p: 4 
          }}>
            <IconButton
              aria-label="close"
              onClick={handleFormClose}
              sx={{
                position: 'absolute',
                right: 8,
                top: 8,
                color: (theme) => theme.palette.grey[500],
              }}
            >
              <CloseIcon />
            </IconButton>
            <SingleTenantForm
              tenant={currentTenant}
              onSave={handleFormSubmit}
              onCancel={handleFormClose}
              isSubmitting={isSubmitting}
              submitError={submitError}
            />
          </Box>
        </Modal>
        
        {/* Delete Confirmation Dialog */}
        <Dialog
          open={deleteDialogOpen}
          onClose={() => setDeleteDialogOpen(false)}
          aria-labelledby="delete-tenant-dialog"
        >
          <DialogTitle id="delete-tenant-dialog">
            Confirm Deletion
          </DialogTitle>
          <DialogContent>
            <Typography>
              Are you sure you want to delete tenant "{tenantToDelete?.name}"? This action cannot be undone.
            </Typography>
          </DialogContent>
          <DialogActions>
            <Button 
              onClick={() => setDeleteDialogOpen(false)} 
              color="primary"
            >
              Cancel
            </Button>
            <Button 
              onClick={handleDeleteTenant} 
              color="error" 
              variant="contained"
            >
              Delete
            </Button>
          </DialogActions>
        </Dialog>
        
        {/* API Key Dialog */}
        <Dialog
          open={apiKeyDialogOpen}
          onClose={() => {
            setApiKeyDialogOpen(false);
            setCurrentApiKey(null);
          }}
          aria-labelledby="api-key-dialog"
        >
          <DialogTitle id="api-key-dialog">
            API Key Generated
          </DialogTitle>
          <DialogContent>
            <Typography sx={{ mb: 2 }}>
              This API key will only be shown once. Make sure to save it in a secure location.
            </Typography>
            <Box sx={{ 
              p: 2, 
              bgcolor: 'background.default', 
              borderRadius: 1,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between'
            }}>
              <Typography
                sx={{
                  fontFamily: 'monospace',
                  overflowWrap: 'break-word',
                  maxWidth: '400px'
                }}
              >
                {currentApiKey}
              </Typography>
              <IconButton onClick={handleCopyApiKey} color="primary">
                <CopyIcon />
              </IconButton>
            </Box>
          </DialogContent>
          <DialogActions>
            <Button 
              onClick={() => {
                setApiKeyDialogOpen(false);
                setCurrentApiKey(null);
              }} 
              color="primary"
            >
              Close
            </Button>
          </DialogActions>
        </Dialog>
        
        {/* Snackbar for notifications */}
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

export default TenantsAdmin;
