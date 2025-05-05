import React, { useState, useEffect } from 'react';
import {
  Badge,
  IconButton,
  Menu,
  MenuItem,
  Typography,
  Box,
  Divider,
  ListItemIcon,
  ListItemText,
  List,
  ListItem,
  Tooltip,
  Chip,
} from '@mui/material';
import {
  Notifications as NotificationsIcon,
  Info as InfoIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  CheckCircle as CheckCircleIcon,
  Delete as DeleteIcon,
  DoneAll as DoneAllIcon,
} from '@mui/icons-material';
import { pluginManager } from '../plugins';

// Define type for notifications from the plugin
interface Notification {
  id: string;
  title: string;
  message: string;
  type: 'info' | 'success' | 'warning' | 'error';
  timestamp: Date;
  read: boolean;
}

const NotificationCenter: React.FC = () => {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const open = Boolean(anchorEl);

  // Get notifications on mount and set up event listeners
  useEffect(() => {
    const notificationPlugin = pluginManager.getPlugin('notifications');
    
    if (!notificationPlugin) {
      console.error('Notification plugin not found');
      return;
    }
    
    // Initial load of notifications
    setNotifications(notificationPlugin.getAllNotifications());
    
    // Set up event listeners for notification changes
    const handleNewNotification = (event: CustomEvent<Notification>) => {
      setNotifications(prevNotifications => [event.detail, ...prevNotifications]);
    };
    
    const handleUpdatedNotification = (event: CustomEvent<Notification>) => {
      setNotifications(prevNotifications => 
        prevNotifications.map(notification => 
          notification.id === event.detail.id ? event.detail : notification
        )
      );
    };
    
    const handleDeletedNotification = (event: CustomEvent<Notification>) => {
      setNotifications(prevNotifications => 
        prevNotifications.filter(notification => notification.id !== event.detail.id)
      );
    };
    
    // Add event listeners
    window.addEventListener('notification:new', handleNewNotification as EventListener);
    window.addEventListener('notification:updated', handleUpdatedNotification as EventListener);
    window.addEventListener('notification:deleted', handleDeletedNotification as EventListener);
    
    // Clean up event listeners on unmount
    return () => {
      window.removeEventListener('notification:new', handleNewNotification as EventListener);
      window.removeEventListener('notification:updated', handleUpdatedNotification as EventListener);
      window.removeEventListener('notification:deleted', handleDeletedNotification as EventListener);
    };
  }, []);
  
  // Handle menu open/close
  const handleClick = (event: React.MouseEvent<HTMLButtonElement>) => {
    setAnchorEl(event.currentTarget);
  };
  
  const handleClose = () => {
    setAnchorEl(null);
  };
  
  // Handle notification actions
  const handleMarkAsRead = (notificationId: string) => {
    const notificationPlugin = pluginManager.getPlugin('notifications');
    
    if (notificationPlugin) {
      notificationPlugin.markAsRead(notificationId);
    }
  };
  
  const handleDelete = (notificationId: string) => {
    const notificationPlugin = pluginManager.getPlugin('notifications');
    
    if (notificationPlugin) {
      notificationPlugin.deleteNotification(notificationId);
    }
  };
  
  const handleMarkAllAsRead = () => {
    const notificationPlugin = pluginManager.getPlugin('notifications');
    
    if (notificationPlugin) {
      notifications.forEach(notification => {
        if (!notification.read) {
          notificationPlugin.markAsRead(notification.id);
        }
      });
    }
  };
  
  // Count unread notifications
  const unreadCount = notifications.filter(n => !n.read).length;
  
  // Get notification icon based on type
  const getNotificationIcon = (type: 'info' | 'success' | 'warning' | 'error') => {
    switch (type) {
      case 'info':
        return <InfoIcon color="info" />;
      case 'success':
        return <CheckCircleIcon color="success" />;
      case 'warning':
        return <WarningIcon color="warning" />;
      case 'error':
        return <ErrorIcon color="error" />;
      default:
        return <InfoIcon />;
    }
  };
  
  // Format notification timestamp
  const formatTimestamp = (timestamp: Date) => {
    const now = new Date();
    const diff = now.getTime() - timestamp.getTime();
    
    // If less than a minute ago
    if (diff < 60000) {
      return 'Just now';
    }
    
    // If less than an hour ago
    if (diff < 3600000) {
      const minutes = Math.floor(diff / 60000);
      return `${minutes} minute${minutes > 1 ? 's' : ''} ago`;
    }
    
    // If less than a day ago
    if (diff < 86400000) {
      const hours = Math.floor(diff / 3600000);
      return `${hours} hour${hours > 1 ? 's' : ''} ago`;
    }
    
    // If less than a week ago
    if (diff < 604800000) {
      const days = Math.floor(diff / 86400000);
      return `${days} day${days > 1 ? 's' : ''} ago`;
    }
    
    // Otherwise, return the date
    return timestamp.toLocaleDateString();
  };
  
  return (
    <>
      <Tooltip title="Notifications">
        <IconButton
          onClick={handleClick}
          size="large"
          aria-controls={open ? 'notifications-menu' : undefined}
          aria-haspopup="true"
          aria-expanded={open ? 'true' : undefined}
          color="inherit"
        >
          <Badge badgeContent={unreadCount} color="error">
            <NotificationsIcon />
          </Badge>
        </IconButton>
      </Tooltip>
      
      <Menu
        id="notifications-menu"
        anchorEl={anchorEl}
        open={open}
        onClose={handleClose}
        MenuListProps={{
          'aria-labelledby': 'notifications-button',
        }}
        PaperProps={{
          style: {
            width: '320px',
            maxHeight: '500px',
          },
        }}
      >
        <Box sx={{ px: 2, py: 1, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="h6">Notifications</Typography>
          
          {unreadCount > 0 && (
            <Tooltip title="Mark All as Read">
              <IconButton size="small" onClick={handleMarkAllAsRead}>
                <DoneAllIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          )}
        </Box>
        
        <Divider />
        
        {notifications.length === 0 ? (
          <Box sx={{ p: 2, textAlign: 'center' }}>
            <Typography variant="body2" color="text.secondary">
              No notifications
            </Typography>
          </Box>
        ) : (
          <List sx={{ px: 0, py: 0 }}>
            {notifications.map((notification) => (
              <React.Fragment key={notification.id}>
                <ListItem
                  sx={{
                    py: 1,
                    backgroundColor: notification.read ? 'transparent' : 'action.hover',
                  }}
                  secondaryAction={
                    <IconButton
                      edge="end"
                      aria-label="delete"
                      size="small"
                      onClick={() => handleDelete(notification.id)}
                    >
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  }
                >
                  <ListItemIcon sx={{ minWidth: 40 }}>
                    {getNotificationIcon(notification.type)}
                  </ListItemIcon>
                  
                  <ListItemText
                    primary={
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="body2" sx={{ fontWeight: notification.read ? 'normal' : 'bold' }}>
                          {notification.title}
                        </Typography>
                        
                        {!notification.read && (
                          <Chip
                            label="New"
                            color="primary"
                            size="small"
                            sx={{ height: 20, fontSize: '0.7rem' }}
                            onClick={() => handleMarkAsRead(notification.id)}
                          />
                        )}
                      </Box>
                    }
                    secondary={
                      <>
                        <Typography variant="body2" color="text.secondary" sx={{ fontSize: '0.8rem' }}>
                          {notification.message}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {formatTimestamp(notification.timestamp)}
                        </Typography>
                      </>
                    }
                  />
                </ListItem>
                <Divider component="li" />
              </React.Fragment>
            ))}
          </List>
        )}
      </Menu>
    </>
  );
};

export default NotificationCenter;