import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import {
  AppBar,
  Box,
  Container,
  Drawer,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  Divider,
  Avatar,
  Menu,
  MenuItem,
  Chip,
} from '@mui/material';
import {
  Menu as MenuIcon,
  Dashboard as DashboardIcon,
  Work as WorkIcon,
  Logout as LogoutIcon,
  AccountCircle,
  Person as PersonIcon,
  Shield as ShieldIcon,
  AdminPanelSettings as AdminIcon,
  Group as UsersIcon,
  VpnKey as RolesIcon,
  Schedule as JobsAdminIcon,
} from '@mui/icons-material';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../auth';
import { ROUTES } from '../config/constants';
import NotificationCenter from './NotificationCenter';
import ThemeToggler from './ThemeToggler';

const drawerWidth = 240;

const Layout: React.FC = () => {
  const [mobileOpen, setMobileOpen] = useState(false);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const { user, logout, hasRole } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handleDrawerToggle = () => {
    setMobileOpen(!mobileOpen);
  };

  const handleProfileMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleLogout = async () => {
    handleMenuClose();
    await logout();
    navigate('/login');
  };

  // Get the highest role for display
  const getHighestRole = (): string => {
    if (!user || !user.roles || user.roles.length === 0) return 'User';
    
    if (user.roles.includes('admin')) return 'Administrator';
    if (user.roles.includes('operator')) return 'Operator';
    if (user.roles.includes('viewer')) return 'Viewer';
    
    return 'User';
  };

  // Get color for role chip
  const getRoleColor = (): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' => {
    if (!user || !user.roles || user.roles.length === 0) return 'default';
    
    if (user.roles.includes('admin')) return 'error';
    if (user.roles.includes('operator')) return 'warning';
    if (user.roles.includes('viewer')) return 'info';
    
    return 'default';
  };

  const drawer = (
    <div>
      <Toolbar
        sx={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'flex-start',
          py: 2,
        }}
      >
        <Typography variant="h6" noWrap component="div" sx={{ mb: 1 }}>
          Deployment Portal
        </Typography>
        
        {user && (
          <Box sx={{ display: 'flex', alignItems: 'center', mt: 1 }}>
            <Chip
              size="small"
              label={getHighestRole()}
              color={getRoleColor()}
              sx={{ fontSize: '0.75rem' }}
            />
          </Box>
        )}
      </Toolbar>
      <Divider />
      <List>
        <ListItem disablePadding selected={location.pathname === '/'}>
          <ListItemButton component={Link} to="/">
            <ListItemIcon>
              <DashboardIcon />
            </ListItemIcon>
            <ListItemText primary="Dashboard" />
          </ListItemButton>
        </ListItem>
        
        {(hasRole(['admin', 'operator', 'viewer'])) && (
          <ListItem disablePadding selected={location.pathname.startsWith('/jobs')}>
            <ListItemButton component={Link} to="/jobs">
              <ListItemIcon>
                <WorkIcon />
              </ListItemIcon>
              <ListItemText primary="Jobs" />
            </ListItemButton>
          </ListItem>
        )}
      </List>
      
      {/* Admin Section */}
      {hasRole(['admin']) && (
        <>
          <Divider sx={{ my: 1 }} />
          <ListItem sx={{ py: 1 }}>
            <ListItemIcon>
              <AdminIcon />
            </ListItemIcon>
            <ListItemText primary="Admin" />
          </ListItem>
          
          <List component="div" disablePadding>
            <ListItem disablePadding selected={location.pathname === ROUTES.ADMIN.USERS}>
              <ListItemButton component={Link} to={ROUTES.ADMIN.USERS} sx={{ pl: 4 }}>
                <ListItemIcon>
                  <UsersIcon />
                </ListItemIcon>
                <ListItemText primary="Users" />
              </ListItemButton>
            </ListItem>
            
            <ListItem disablePadding selected={location.pathname === ROUTES.ADMIN.ROLES}>
              <ListItemButton component={Link} to={ROUTES.ADMIN.ROLES} sx={{ pl: 4 }}>
                <ListItemIcon>
                  <RolesIcon />
                </ListItemIcon>
                <ListItemText primary="Roles" />
              </ListItemButton>
            </ListItem>
            
            <ListItem disablePadding selected={location.pathname === ROUTES.ADMIN.JOBS}>
              <ListItemButton component={Link} to={ROUTES.ADMIN.JOBS} sx={{ pl: 4 }}>
                <ListItemIcon>
                  <JobsAdminIcon />
                </ListItemIcon>
                <ListItemText primary="Job Management" />
              </ListItemButton>
            </ListItem>
          </List>
        </>
      )}
    </div>
  );

  const isMenuOpen = Boolean(anchorEl);
  const menuId = 'primary-search-account-menu';
  const renderMenu = (
    <Menu
      anchorEl={anchorEl}
      anchorOrigin={{
        vertical: 'bottom',
        horizontal: 'right',
      }}
      id={menuId}
      keepMounted
      transformOrigin={{
        vertical: 'top',
        horizontal: 'right',
      }}
      open={isMenuOpen}
      onClose={handleMenuClose}
    >
      {user && (
        <>
          <MenuItem disabled sx={{ opacity: 1 }}>
            <Box sx={{ display: 'flex', flexDirection: 'column' }}>
              <Typography variant="body2" fontWeight="bold">
                {user.username}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {user.email}
              </Typography>
            </Box>
          </MenuItem>
          
          <MenuItem disabled sx={{ opacity: 1 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <ShieldIcon fontSize="small" color={getRoleColor()} />
              <Typography variant="body2">
                {getHighestRole()}
              </Typography>
            </Box>
          </MenuItem>
          
          <Divider />
        </>
      )}
      
      <MenuItem onClick={handleLogout}>
        <ListItemIcon>
          <LogoutIcon fontSize="small" />
        </ListItemIcon>
        Logout
      </MenuItem>
    </Menu>
  );

  return (
    <Box sx={{ display: 'flex' }}>
      <AppBar
        position="fixed"
        sx={{
          width: { sm: `calc(100% - ${drawerWidth}px)` },
          ml: { sm: `${drawerWidth}px` },
        }}
      >
        <Toolbar>
          <IconButton
            color="inherit"
            aria-label="open drawer"
            edge="start"
            onClick={handleDrawerToggle}
            sx={{ mr: 2, display: { sm: 'none' } }}
          >
            <MenuIcon />
          </IconButton>
          
          <Typography variant="h6" noWrap component="div" sx={{ flexGrow: 1 }}>
            {location.pathname === '/' && 'Dashboard'}
            {location.pathname === '/jobs' && 'Jobs'}
            {location.pathname.match(/^\/jobs\/[^/]+$/) && 'Job Details'}
            {location.pathname === ROUTES.ADMIN.USERS && 'User Management'}
            {location.pathname === ROUTES.ADMIN.ROLES && 'Role Management'}
            {location.pathname === ROUTES.ADMIN.JOBS && 'Job Management'}
          </Typography>
          
          {/* Theme Toggler */}
          <ThemeToggler />
          
          {/* Notification Center */}
          <NotificationCenter />
          
          {/* User menu */}
          <IconButton
            size="large"
            edge="end"
            aria-label="account of current user"
            aria-controls={menuId}
            aria-haspopup="true"
            onClick={handleProfileMenuOpen}
            color="inherit"
          >
            {user ? (
              <Avatar
                sx={{ 
                  width: 32, 
                  height: 32,
                  bgcolor: getRoleColor() + '.main'
                }}
              >
                {user.username.charAt(0).toUpperCase()}
              </Avatar>
            ) : (
              <AccountCircle />
            )}
          </IconButton>
        </Toolbar>
      </AppBar>
      {renderMenu}
      
      <Box
        component="nav"
        sx={{ width: { sm: drawerWidth }, flexShrink: { sm: 0 } }}
        aria-label="mailbox folders"
      >
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={handleDrawerToggle}
          ModalProps={{
            keepMounted: true, // Better open performance on mobile.
          }}
          sx={{
            display: { xs: 'block', sm: 'none' },
            '& .MuiDrawer-paper': { boxSizing: 'border-box', width: drawerWidth },
          }}
        >
          {drawer}
        </Drawer>
        <Drawer
          variant="permanent"
          sx={{
            display: { xs: 'none', sm: 'block' },
            '& .MuiDrawer-paper': { boxSizing: 'border-box', width: drawerWidth },
          }}
          open
        >
          {drawer}
        </Drawer>
      </Box>
      
      <Box
        component="main"
        sx={{ 
          flexGrow: 1, 
          p: 3, 
          width: { sm: `calc(100% - ${drawerWidth}px)` },
          minHeight: '100vh',
          backgroundColor: 'background.default'
        }}
      >
        <Toolbar />
        <Container maxWidth="lg" sx={{ mt: 2, mb: 4 }}>
          <Outlet />
        </Container>
      </Box>
    </Box>
  );
};

export default Layout;