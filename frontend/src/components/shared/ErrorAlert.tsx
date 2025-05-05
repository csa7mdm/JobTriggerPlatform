import React from 'react';
import { Alert, AlertTitle, Box } from '@mui/material';

interface ErrorAlertProps {
  message: string;
  title?: string;
  severity?: 'error' | 'warning' | 'info' | 'success';
  onClose?: () => void;
}

const ErrorAlert: React.FC<ErrorAlertProps> = ({ 
  message, 
  title, 
  severity = 'error',
  onClose 
}) => {
  return (
    <Box sx={{ mb: 3 }}>
      <Alert severity={severity} onClose={onClose}>
        {title && <AlertTitle>{title}</AlertTitle>}
        {message}
      </Alert>
    </Box>
  );
};

export default ErrorAlert;