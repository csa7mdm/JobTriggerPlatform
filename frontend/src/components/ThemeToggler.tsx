import React, { useState, useEffect } from 'react';
import {
  IconButton,
  Menu,
  MenuItem,
  ListItemIcon,
  ListItemText,
  Tooltip,
} from '@mui/material';
import {
  Brightness4 as DarkModeIcon,
  Brightness7 as LightModeIcon,
  BrightnessAuto as AutoModeIcon,
} from '@mui/icons-material';
import { pluginManager, lazyLoadPlugin } from '../plugins';

const ThemeToggler: React.FC = () => {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [currentTheme, setCurrentTheme] = useState<'light' | 'dark' | 'system'>('system');
  const [effectiveTheme, setEffectiveTheme] = useState<'light' | 'dark'>('light');
  const [pluginLoaded, setPluginLoaded] = useState(false);
  
  const open = Boolean(anchorEl);
  
  // Load theme plugin and set up event listeners
  useEffect(() => {
    const initializeThemePlugin = async () => {
      // Try to get the plugin first
      let themePlugin = pluginManager.getPlugin('theme');
      
      // If not loaded, try to lazy load it
      if (!themePlugin) {
        themePlugin = await lazyLoadPlugin('theme');
      }
      
      if (themePlugin) {
        setPluginLoaded(true);
        setCurrentTheme(themePlugin.getTheme());
        setEffectiveTheme(themePlugin.getEffectiveTheme());
        
        // Listen for theme changes
        const handleThemeChange = (event: CustomEvent<{ theme: string, effectiveTheme: 'light' | 'dark' }>) => {
          setCurrentTheme(event.detail.theme as 'light' | 'dark' | 'system');
          setEffectiveTheme(event.detail.effectiveTheme);
        };
        
        window.addEventListener('theme:changed', handleThemeChange as EventListener);
        
        return () => {
          window.removeEventListener('theme:changed', handleThemeChange as EventListener);
        };
      }
    };
    
    initializeThemePlugin();
  }, []);
  
  // Handle menu open/close
  const handleClick = (event: React.MouseEvent<HTMLButtonElement>) => {
    setAnchorEl(event.currentTarget);
  };
  
  const handleClose = () => {
    setAnchorEl(null);
  };
  
  // Handle theme selection
  const handleThemeSelect = (theme: 'light' | 'dark' | 'system') => {
    const themePlugin = pluginManager.getPlugin('theme');
    
    if (themePlugin) {
      themePlugin.setTheme(theme);
    }
    
    handleClose();
  };
  
  // If plugin is not loaded, show nothing
  if (!pluginLoaded) {
    return null;
  }
  
  // Determine which icon to show based on the effective theme
  const getThemeIcon = () => {
    switch (currentTheme) {
      case 'light':
        return <LightModeIcon />;
      case 'dark':
        return <DarkModeIcon />;
      case 'system':
        return effectiveTheme === 'light' ? <LightModeIcon /> : <DarkModeIcon />;
      default:
        return <LightModeIcon />;
    }
  };
  
  return (
    <>
      <Tooltip title="Change theme">
        <IconButton
          onClick={handleClick}
          size="large"
          aria-controls={open ? 'theme-menu' : undefined}
          aria-haspopup="true"
          aria-expanded={open ? 'true' : undefined}
          color="inherit"
        >
          {getThemeIcon()}
        </IconButton>
      </Tooltip>
      
      <Menu
        id="theme-menu"
        anchorEl={anchorEl}
        open={open}
        onClose={handleClose}
        MenuListProps={{
          'aria-labelledby': 'theme-button',
        }}
      >
        <MenuItem
          selected={currentTheme === 'light'}
          onClick={() => handleThemeSelect('light')}
        >
          <ListItemIcon>
            <LightModeIcon fontSize="small" />
          </ListItemIcon>
          <ListItemText>Light Mode</ListItemText>
        </MenuItem>
        
        <MenuItem
          selected={currentTheme === 'dark'}
          onClick={() => handleThemeSelect('dark')}
        >
          <ListItemIcon>
            <DarkModeIcon fontSize="small" />
          </ListItemIcon>
          <ListItemText>Dark Mode</ListItemText>
        </MenuItem>
        
        <MenuItem
          selected={currentTheme === 'system'}
          onClick={() => handleThemeSelect('system')}
        >
          <ListItemIcon>
            <AutoModeIcon fontSize="small" />
          </ListItemIcon>
          <ListItemText>System Default</ListItemText>
        </MenuItem>
      </Menu>
    </>
  );
};

export default ThemeToggler;