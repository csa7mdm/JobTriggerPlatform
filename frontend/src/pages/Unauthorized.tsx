import React from 'react';
import { Box, Button, Typography, Paper } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { LockOutlined as LockIcon } from '@mui/icons-material';
import { useAuth } from '../auth';

const Unauthorized: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();

  return (
    <Box
      display="flex"
      flexDirection="column"
      alignItems="center"
      justifyContent="center"
      minHeight="80vh"
      textAlign="center"
      px={2}
    >
      <Paper
        elevation={3}
        sx={{
          p: 4,
          maxWidth: 500,
          width: '100%',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
        }}
      >
        <LockIcon sx={{ fontSize: 60, color: 'error.main', mb: 2 }} />
        
        <Typography variant="h4" gutterBottom>
          Access Denied
        </Typography>
        
        <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
          You don't have the necessary permissions to access this page.
        </Typography>
        
        {user && (
          <Box sx={{ mb: 3, textAlign: 'left', width: '100%' }}>
            <Typography variant="subtitle2">Your current access:</Typography>
            <Typography variant="body2">User: {user.email}</Typography>
            <Typography variant="body2">
              Roles: {user.roles.length > 0 ? user.roles.join(', ') : 'None'}
            </Typography>
          </Box>
        )}
        
        <Box sx={{ display: 'flex', gap: 2, mt: 2 }}>
          <Button variant="outlined" onClick={() => navigate(-1)}>
            Go Back
          </Button>
          
          <Button
            variant="contained"
            color="primary"
            onClick={() => navigate('/')}
          >
            Go to Dashboard
          </Button>
        </Box>
      </Paper>
    </Box>
  );
};

export default Unauthorized;