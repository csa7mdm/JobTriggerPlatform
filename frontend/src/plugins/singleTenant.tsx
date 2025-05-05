import React from 'react';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { 
  Box,
  TextField,
  Button,
  Typography,
  Paper,
  Grid,
  FormHelperText,
  Switch,
  FormControlLabel
} from '@mui/material';

// Zod schema that mirrors server parameters
const singleTenantSchema = z.object({
  tenantId: z.string()
    .min(3, 'Tenant ID must be at least 3 characters')
    .max(50, 'Tenant ID cannot exceed 50 characters'),
  displayName: z.string()
    .min(3, 'Display name must be at least 3 characters')
    .max(100, 'Display name cannot exceed 100 characters'),
  description: z.string().optional(),
  connectionString: z.string()
    .min(10, 'Connection string is too short')
    .includes('Data Source=', { message: 'Must be a valid connection string' }),
  isActive: z.boolean().default(true),
  maxConcurrentJobs: z.number()
    .int('Must be an integer')
    .min(1, 'At least 1 concurrent job required')
    .max(100, 'Cannot exceed 100 concurrent jobs'),
  apiKey: z.string()
    .min(32, 'API key must be at least 32 characters')
    .optional()
});

// TypeScript type derived from Zod schema
type SingleTenantFormData = z.infer<typeof singleTenantSchema>;

// Default values for the form
const defaultValues: SingleTenantFormData = {
  tenantId: '',
  displayName: '',
  description: '',
  connectionString: 'Data Source=',
  isActive: true,
  maxConcurrentJobs: 5,
  apiKey: ''
};

interface SingleTenantFormProps {
  onSubmit: (data: SingleTenantFormData) => void;
  initialData?: Partial<SingleTenantFormData>;
  isLoading?: boolean;
}

const SingleTenantForm: React.FC<SingleTenantFormProps> = ({
  onSubmit,
  initialData = {},
  isLoading = false
}) => {
  const {
    control,
    handleSubmit,
    formState: { errors },
    reset
  } = useForm<SingleTenantFormData>({
    resolver: zodResolver(singleTenantSchema),
    defaultValues: { ...defaultValues, ...initialData }
  });

  const submitHandler = (data: SingleTenantFormData) => {
    onSubmit(data);
  };

  React.useEffect(() => {
    if (initialData) {
      reset({ ...defaultValues, ...initialData });
    }
  }, [initialData, reset]);

  return (
    <Paper elevation={3} sx={{ p: 3, my: 2 }}>
      <Typography variant="h5" component="h2" gutterBottom>
        Single Tenant Configuration
      </Typography>
      
      <Box component="form" onSubmit={handleSubmit(submitHandler)} noValidate>
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Controller
              name="tenantId"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  label="Tenant ID"
                  variant="outlined"
                  fullWidth
                  required
                  error={!!errors.tenantId}
                  helperText={errors.tenantId?.message}
                  disabled={isLoading}
                />
              )}
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <Controller
              name="displayName"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  label="Display Name"
                  variant="outlined"
                  fullWidth
                  required
                  error={!!errors.displayName}
                  helperText={errors.displayName?.message}
                  disabled={isLoading}
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
                  disabled={isLoading}
                />
              )}
            />
          </Grid>
          
          <Grid item xs={12}>
            <Controller
              name="connectionString"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  label="Connection String"
                  variant="outlined"
                  fullWidth
                  required
                  error={!!errors.connectionString}
                  helperText={errors.connectionString?.message}
                  disabled={isLoading}
                />
              )}
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <Controller
              name="maxConcurrentJobs"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  label="Max Concurrent Jobs"
                  variant="outlined"
                  fullWidth
                  required
                  type="number"
                  inputProps={{ min: 1, max: 100 }}
                  error={!!errors.maxConcurrentJobs}
                  helperText={errors.maxConcurrentJobs?.message}
                  disabled={isLoading}
                  onChange={(e) => field.onChange(parseInt(e.target.value, 10) || '')}
                />
              )}
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <Controller
              name="apiKey"
              control={control}
              render={({ field }) => (
                <TextField
                  {...field}
                  label="API Key"
                  variant="outlined"
                  fullWidth
                  error={!!errors.apiKey}
                  helperText={errors.apiKey?.message}
                  disabled={isLoading}
                />
              )}
            />
          </Grid>
          
          <Grid item xs={12}>
            <Controller
              name="isActive"
              control={control}
              render={({ field }) => (
                <FormControlLabel
                  control={
                    <Switch
                      checked={field.value}
                      onChange={(e) => field.onChange(e.target.checked)}
                      disabled={isLoading}
                    />
                  }
                  label="Active Tenant"
                />
              )}
            />
            {errors.isActive && (
              <FormHelperText error>{errors.isActive.message}</FormHelperText>
            )}
          </Grid>
          
          <Grid item xs={12}>
            <Button
              type="submit"
              variant="contained"
              color="primary"
              disabled={isLoading}
              sx={{ mt: 2 }}
            >
              {isLoading ? 'Saving...' : 'Save Tenant'}
            </Button>
            <Button
              type="button"
              variant="outlined"
              onClick={() => reset(defaultValues)}
              disabled={isLoading}
              sx={{ mt: 2, ml: 2 }}
            >
              Reset
            </Button>
          </Grid>
        </Grid>
      </Box>
    </Paper>
  );
};

export default SingleTenantForm;