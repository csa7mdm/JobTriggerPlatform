import React, { useState, useEffect } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  Box,
  Button,
  TextField,
  FormControlLabel,
  Switch,
  Typography,
  Card,
  CardContent,
  Grid,
  Autocomplete,
  Chip,
  FormHelperText,
  Divider,
  CircularProgress,
  Alert,
} from '@mui/material';
import { Save as SaveIcon, Cancel as CancelIcon } from '@mui/icons-material';
import { Tenant } from '../../types/tenant';
import { jobsApi } from '../../api/client';

// Define a schema for tenant validation
const tenantSchema = z.object({
  name: z.string().min(3, 'Name must be at least 3 characters').max(100),
  description: z.string().max(500, 'Description cannot exceed 500 characters').optional(),
  contactEmail: z.string().email('Invalid email address'),
  contactName: z.string().min(2, 'Contact name must be at least 2 characters'),
  contactPhone: z.string().optional(),
  isActive: z.boolean(),
  allowedJobs: z.array(z.string()),
  settings: z.object({
    logoUrl: z.string().url('Invalid URL').optional().or(z.literal('')),
    primaryColor: z.string().regex(/^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$/, 'Invalid color format').optional().or(z.literal('')),
    allowApiAccess: z.boolean(),
    maxConcurrentJobs: z.number().int().min(1).max(100),
    notificationEmail: z.string().email('Invalid email address').optional().or(z.literal('')),
    webhookUrl: z.string().url('Invalid URL').optional().or(z.literal('')),
  }).optional()
});

type TenantFormData = z.infer<typeof tenantSchema>;

interface SingleTenantFormProps {
  tenant?: Tenant;
  onSave: (data: TenantFormData) => Promise<void>;
  onCancel: () => void;
  isSubmitting?: boolean;
  submitError?: string | null;
}

const SingleTenantForm: React.FC<SingleTenantFormProps> = ({
  tenant,
  onSave,
  onCancel,
  isSubmitting = false,
  submitError = null,
}) => {
  const [availableJobs, setAvailableJobs] = useState<{ id: string; name: string; description: string }[]>([]);
  const [isLoadingJobs, setIsLoadingJobs] = useState(false);
  const [jobsError, setJobsError] = useState<string | null>(null);

  // Initialize form with default values or tenant data if editing
  const defaultValues: TenantFormData = tenant
    ? {
        ...tenant,
        settings: tenant.settings || {
          allowApiAccess: false,
          maxConcurrentJobs: 5,
        },
      }
    : {
        name: '',
        description: '',
        contactEmail: '',
        contactName: '',
        contactPhone: '',
        isActive: true,
        allowedJobs: [],
        settings: {
          logoUrl: '',
          primaryColor: '#1976d2',
          allowApiAccess: false,
          maxConcurrentJobs: 5,
          notificationEmail: '',
          webhookUrl: '',
        },
      };

  const {
    control,
    handleSubmit,
    formState: { errors },
    watch,
    reset,
  } = useForm<TenantFormData>({
    resolver: zodResolver(tenantSchema),
    defaultValues,
  });

  // Load available jobs
  useEffect(() => {
    const fetchJobs = async () => {
      setIsLoadingJobs(true);
      try {
        const response = await jobsApi.getAll();
        setAvailableJobs(
          response.data.map((job: any) => ({
            id: job.id,
            name: job.name,
            description: job.description,
          }))
        );
        setJobsError(null);
      } catch (error) {
        console.error('Error fetching jobs:', error);
        setJobsError('Failed to load available jobs');
      } finally {
        setIsLoadingJobs(false);
      }
    };

    fetchJobs();
  }, []);

  // Reset form when tenant prop changes (e.g., when switching from create to edit mode)
  useEffect(() => {
    if (tenant) {
      reset({
        ...tenant,
        settings: tenant.settings || {
          allowApiAccess: false,
          maxConcurrentJobs: 5,
        },
      });
    }
  }, [tenant, reset]);

  const watchAllowApiAccess = watch('settings.allowApiAccess');

  const onSubmit = async (data: TenantFormData) => {
    await onSave(data);
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <Card elevation={3}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            {tenant ? 'Edit Tenant' : 'Create New Tenant'}
          </Typography>

          {submitError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {submitError}
            </Alert>
          )}

          <Grid container spacing={3}>
            {/* Basic Information */}
            <Grid item xs={12}>
              <Typography variant="subtitle1" fontWeight="bold">
                Basic Information
              </Typography>
              <Divider sx={{ mt: 1, mb: 2 }} />
            </Grid>

            <Grid item xs={12} md={6}>
              <Controller
                name="name"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Tenant Name"
                    variant="outlined"
                    fullWidth
                    error={!!errors.name}
                    helperText={errors.name?.message}
                    required
                  />
                )}
              />
            </Grid>

            <Grid item xs={12} md={6}>
              <Controller
                name="isActive"
                control={control}
                render={({ field }) => (
                  <FormControlLabel
                    control={
                      <Switch
                        checked={field.value}
                        onChange={(e) => field.onChange(e.target.checked)}
                      />
                    }
                    label="Active"
                  />
                )}
              />
            </Grid>

            <Grid item xs={12}>
              <Controller
                name="description"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Description"
                    variant="outlined"
                    fullWidth
                    multiline
                    rows={3}
                    error={!!errors.description}
                    helperText={errors.description?.message}
                  />
                )}
              />
            </Grid>

            {/* Contact Information */}
            <Grid item xs={12}>
              <Typography variant="subtitle1" fontWeight="bold">
                Contact Information
              </Typography>
              <Divider sx={{ mt: 1, mb: 2 }} />
            </Grid>

            <Grid item xs={12} md={6}>
              <Controller
                name="contactName"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Contact Name"
                    variant="outlined"
                    fullWidth
                    error={!!errors.contactName}
                    helperText={errors.contactName?.message}
                    required
                  />
                )}
              />
            </Grid>

            <Grid item xs={12} md={6}>
              <Controller
                name="contactPhone"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Contact Phone"
                    variant="outlined"
                    fullWidth
                    error={!!errors.contactPhone}
                    helperText={errors.contactPhone?.message}
                  />
                )}
              />
            </Grid>

            <Grid item xs={12}>
              <Controller
                name="contactEmail"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Contact Email"
                    variant="outlined"
                    fullWidth
                    error={!!errors.contactEmail}
                    helperText={errors.contactEmail?.message}
                    required
                  />
                )}
              />
            </Grid>

            {/* Job Access */}
            <Grid item xs={12}>
              <Typography variant="subtitle1" fontWeight="bold">
                Job Access
              </Typography>
              <Divider sx={{ mt: 1, mb: 2 }} />
            </Grid>

            <Grid item xs={12}>
              <Controller
                name="allowedJobs"
                control={control}
                render={({ field }) => (
                  <>
                    <Autocomplete
                      multiple
                      id="allowedJobs"
                      options={availableJobs}
                      getOptionLabel={(option) => option.name}
                      loading={isLoadingJobs}
                      value={availableJobs.filter((job) => field.value.includes(job.id))}
                      onChange={(_, newValue) => {
                        field.onChange(newValue.map((job) => job.id));
                      }}
                      renderTags={(value, getTagProps) =>
                        value.map((option, index) => (
                          <Chip
                            key={option.id}
                            label={option.name}
                            {...getTagProps({ index })}
                          />
                        ))
                      }
                      renderInput={(params) => (
                        <TextField
                          {...params}
                          label="Allowed Jobs"
                          placeholder="Select jobs"
                          error={!!errors.allowedJobs}
                          helperText={errors.allowedJobs?.message}
                          InputProps={{
                            ...params.InputProps,
                            endAdornment: (
                              <>
                                {isLoadingJobs ? <CircularProgress color="inherit" size={20} /> : null}
                                {params.InputProps.endAdornment}
                              </>
                            ),
                          }}
                        />
                      )}
                    />
                    {jobsError && (
                      <FormHelperText error>{jobsError}</FormHelperText>
                    )}
                  </>
                )}
              />
            </Grid>

            {/* Advanced Settings */}
            <Grid item xs={12}>
              <Typography variant="subtitle1" fontWeight="bold">
                Advanced Settings
              </Typography>
              <Divider sx={{ mt: 1, mb: 2 }} />
            </Grid>

            <Grid item xs={12} md={6}>
              <Controller
                name="settings.logoUrl"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Logo URL"
                    variant="outlined"
                    fullWidth
                    error={!!errors.settings?.logoUrl}
                    helperText={errors.settings?.logoUrl?.message}
                  />
                )}
              />
            </Grid>

            <Grid item xs={12} md={6}>
              <Controller
                name="settings.primaryColor"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Primary Color"
                    variant="outlined"
                    fullWidth
                    error={!!errors.settings?.primaryColor}
                    helperText={errors.settings?.primaryColor?.message}
                    placeholder="#1976d2"
                  />
                )}
              />
            </Grid>

            <Grid item xs={12} md={6}>
              <Controller
                name="settings.allowApiAccess"
                control={control}
                render={({ field }) => (
                  <FormControlLabel
                    control={
                      <Switch
                        checked={field.value}
                        onChange={(e) => field.onChange(e.target.checked)}
                      />
                    }
                    label="Allow API Access"
                  />
                )}
              />
            </Grid>

            <Grid item xs={12} md={6}>
              <Controller
                name="settings.maxConcurrentJobs"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Max Concurrent Jobs"
                    variant="outlined"
                    fullWidth
                    type="number"
                    inputProps={{ min: 1, max: 100 }}
                    error={!!errors.settings?.maxConcurrentJobs}
                    helperText={errors.settings?.maxConcurrentJobs?.message}
                    onChange={(e) => field.onChange(parseInt(e.target.value) || 1)}
                  />
                )}
              />
            </Grid>

            {watchAllowApiAccess && (
              <>
                <Grid item xs={12} md={6}>
                  <Controller
                    name="settings.notificationEmail"
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        label="Notification Email"
                        variant="outlined"
                        fullWidth
                        error={!!errors.settings?.notificationEmail}
                        helperText={errors.settings?.notificationEmail?.message}
                      />
                    )}
                  />
                </Grid>

                <Grid item xs={12} md={6}>
                  <Controller
                    name="settings.webhookUrl"
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        label="Webhook URL"
                        variant="outlined"
                        fullWidth
                        error={!!errors.settings?.webhookUrl}
                        helperText={errors.settings?.webhookUrl?.message}
                      />
                    )}
                  />
                </Grid>
              </>
            )}
          </Grid>

          <Box sx={{ mt: 3, display: 'flex', justifyContent: 'flex-end', gap: 2 }}>
            <Button
              variant="outlined"
              color="secondary"
              onClick={onCancel}
              startIcon={<CancelIcon />}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              variant="contained"
              color="primary"
              startIcon={<SaveIcon />}
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <>
                  <CircularProgress size={24} color="inherit" sx={{ mr:.5 }} />
                  Saving...
                </>
              ) : (
                'Save Tenant'
              )}
            </Button>
          </Box>
        </CardContent>
      </Card>
    </form>
  );
};

export default SingleTenantForm;
