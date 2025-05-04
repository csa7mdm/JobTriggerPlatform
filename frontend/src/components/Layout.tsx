import { AppBar, Box, Toolbar, Typography, Container } from '@mui/material'
import { Outlet } from 'react-router-dom'

const Layout = () => {
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <AppBar position="static">
        <Toolbar>
          <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
            Job Trigger Platform
          </Typography>
        </Toolbar>
      </AppBar>
      
      <Box component="main" sx={{ flexGrow: 1 }}>
        <Outlet />
      </Box>
      
      <Box component="footer" sx={{ py: 2, bgcolor: 'background.paper' }}>
        <Container maxWidth="lg">
          <Typography variant="body2" color="text.secondary" align="center">
            Job Trigger Platform © {new Date().getFullYear()}
          </Typography>
        </Container>
      </Box>
    </Box>
  )
}

export default Layout
