import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  CardHeader,
  Divider,
  Typography,
  CircularProgress,
  Button,
  Skeleton,
  Grid,
  Chip,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  Analytics as AnalyticsIcon,
  TrendingUp as TrendingUpIcon,
  TrendingDown as TrendingDownIcon,
  Info as InfoIcon,
  Check as CheckIcon,
  Error as ErrorIcon,
} from '@mui/icons-material';
import { pluginManager, lazyLoadPlugin } from '../plugins';

interface JobStats {
  totalJobs: number;
  successRate: number;
  failureRate: number;
  averageDuration: number;
  jobsByEnvironment: Record<string, number>;
  mostFrequentTags: Array<{ tag: string; count: number }>;
  trendsData: Array<{
    date: string;
    successCount: number;
    failureCount: number;
  }>;
}

const JobStatsCard: React.FC = () => {
  const [stats, setStats] = useState<JobStats | null>(null);
  const [loading, setLoading] = useState(false);
  const [isPluginLoading, setIsPluginLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pluginLoaded, setPluginLoaded] = useState(false);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);
  
  // Load the plugin and set up event listeners
  useEffect(() => {
    const loadPlugin = async () => {
      setIsPluginLoading(true);
      
      try {
        // Try to get the plugin if it's already loaded
        let statsPlugin = pluginManager.getPlugin('job-stats');
        
        // If not available, try to lazy load it
        if (!statsPlugin) {
          statsPlugin = await lazyLoadPlugin('jobStatsPlugin');
        }
        
        if (statsPlugin) {
          setPluginLoaded(true);
          
          // Get initial stats and last updated time
          setStats(statsPlugin.getStats());
          setLastUpdated(statsPlugin.getLastUpdated());
          
          // Set up event listeners for stats updates
          const handleStatsUpdated = (event: CustomEvent<JobStats>) => {
            setStats(event.detail);
            setLastUpdated(new Date());
            setLoading(false);
          };
          
          const handleStatsError = (event: CustomEvent<string>) => {
            setError(event.detail);
            setLoading(false);
          };
          
          // Add event listeners
          window.addEventListener('job-stats:updated', handleStatsUpdated as EventListener);
          window.addEventListener('job-stats:error', handleStatsError as EventListener);
          
          // Clean up event listeners on unmount
          return () => {
            window.removeEventListener('job-stats:updated', handleStatsUpdated as EventListener);
            window.removeEventListener('job-stats:error', handleStatsError as EventListener);
          };
        } else {
          setError('Failed to load Job Stats plugin');
        }
      } catch (error) {
        console.error('Error loading job stats plugin:', error);
        setError('Failed to load Job Stats plugin');
      } finally {
        setIsPluginLoading(false);
      }
    };
    
    loadPlugin();
  }, []);
  
  // Handle refresh button click
  const handleRefresh = async () => {
    setLoading(true);
    
    try {
      const statsPlugin = pluginManager.getPlugin('job-stats');
      
      if (statsPlugin) {
        await statsPlugin.loadStats();
      } else {
        setError('Job Stats plugin not available');
        setLoading(false);
      }
    } catch (error) {
      console.error('Failed to refresh stats:', error);
      setError('Failed to refresh stats');
      setLoading(false);
    }
  };
  
  // Format duration in seconds to a readable format
  const formatDuration = (seconds: number): string => {
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;
    
    if (minutes === 0) {
      return `${remainingSeconds}s`;
    }
    
    return `${minutes}m ${remainingSeconds}s`;
  };
  
  // Format the last updated time
  const formatLastUpdated = (): string => {
    if (!lastUpdated) return 'Never';
    
    // If less than a minute ago
    const diff = Date.now() - lastUpdated.getTime();
    
    if (diff < 60000) {
      return 'Just now';
    }
    
    // If less than an hour ago
    if (diff < 3600000) {
      const minutes = Math.floor(diff / 60000);
      return `${minutes} minute${minutes !== 1 ? 's' : ''} ago`;
    }
    
    // Otherwise, return time
    return lastUpdated.toLocaleTimeString();
  };
  
  // If the plugin is still loading
  if (isPluginLoading) {
    return (
      <Card>
        <CardHeader 
          title="Job Statistics" 
          avatar={<AnalyticsIcon />} 
        />
        <Divider />
        <CardContent sx={{ minHeight: 250, display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
          <Box sx={{ textAlign: 'center' }}>
            <CircularProgress size={40} sx={{ mb: 2 }} />
            <Typography>Loading statistics plugin...</Typography>
          </Box>
        </CardContent>
      </Card>
    );
  }
  
  // If there was an error loading the plugin
  if (!pluginLoaded) {
    return (
      <Card>
        <CardHeader 
          title="Job Statistics" 
          avatar={<AnalyticsIcon />} 
        />
        <Divider />
        <CardContent sx={{ minHeight: 250, display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
          <Box sx={{ textAlign: 'center' }}>
            <ErrorIcon color="error" sx={{ fontSize: 40, mb: 2 }} />
            <Typography color="error">{error || 'Failed to load statistics plugin'}</Typography>
          </Box>
        </CardContent>
      </Card>
    );
  }
  
  return (
    <Card>
      <CardHeader 
        title="Job Statistics"
        avatar={<AnalyticsIcon />}
        action={
          <Box sx={{ display: 'flex', alignItems: 'center' }}>
            <Typography variant="caption" color="text.secondary" sx={{ mr: 1 }}>
              Updated: {formatLastUpdated()}
            </Typography>
            <Tooltip title="Refresh statistics">
              <IconButton 
                size="small" 
                onClick={handleRefresh}
                disabled={loading}
              >
                {loading ? <CircularProgress size={20} /> : <RefreshIcon />}
              </IconButton>
            </Tooltip>
          </Box>
        }
      />
      <Divider />
      
      <CardContent>
        {loading && !stats ? (
          // Show skeleton when loading initial data
          <Box>
            <Grid container spacing={3}>
              {[1, 2, 3, 4].map((item) => (
                <Grid item xs={6} md={3} key={item}>
                  <Skeleton variant="rectangular" height={40} />
                  <Skeleton variant="text" />
                </Grid>
              ))}
            </Grid>
            <Skeleton variant="rectangular" height={100} sx={{ mt: 3 }} />
          </Box>
        ) : error && !stats ? (
          // Show error message
          <Box sx={{ textAlign: 'center', py: 4 }}>
            <ErrorIcon color="error" sx={{ fontSize: 40, mb: 2 }} />
            <Typography color="error">{error}</Typography>
            <Button
              variant="outlined"
              startIcon={<RefreshIcon />}
              onClick={handleRefresh}
              sx={{ mt: 2 }}
              disabled={loading}
            >
              Try Again
            </Button>
          </Box>
        ) : stats ? (
          // Show the statistics
          <Box>
            <Grid container spacing={3}>
              {/* Success Rate */}
              <Grid item xs={6} md={3}>
                <Box sx={{ textAlign: 'center' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', mb: 1 }}>
                    <Typography variant="h4">
                      {Math.round(stats.successRate * 100)}%
                    </Typography>
                    <CheckIcon color="success" sx={{ ml: 1 }} />
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    Success Rate
                  </Typography>
                </Box>
              </Grid>
              
              {/* Total Jobs */}
              <Grid item xs={6} md={3}>
                <Box sx={{ textAlign: 'center' }}>
                  <Typography variant="h4">
                    {stats.totalJobs}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Total Jobs
                  </Typography>
                </Box>
              </Grid>
              
              {/* Average Duration */}
              <Grid item xs={6} md={3}>
                <Box sx={{ textAlign: 'center' }}>
                  <Typography variant="h4">
                    {formatDuration(stats.averageDuration)}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Avg. Duration
                  </Typography>
                </Box>
              </Grid>
              
              {/* Failure Rate */}
              <Grid item xs={6} md={3}>
                <Box sx={{ textAlign: 'center' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', mb: 1 }}>
                    <Typography variant="h4">
                      {Math.round(stats.failureRate * 100)}%
                    </Typography>
                    <ErrorIcon color="error" sx={{ ml: 1 }} />
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    Failure Rate
                  </Typography>
                </Box>
              </Grid>
            </Grid>
            
            <Divider sx={{ my: 3 }} />
            
            {/* Tags and Environments */}
            <Box>
              <Typography variant="subtitle1" gutterBottom>
                Environments
              </Typography>
              
              <Grid container spacing={1}>
                {Object.entries(stats.jobsByEnvironment).map(([env, count]) => (
                  <Grid item key={env}>
                    <Chip 
                      label={`${env}: ${count}`}
                      color={
                        env === 'production' ? 'error' :
                        env === 'staging' ? 'warning' :
                        env === 'test' ? 'info' :
                        'default'
                      }
                      size="small"
                    />
                  </Grid>
                ))}
              </Grid>
              
              <Typography variant="subtitle1" gutterBottom sx={{ mt: 2 }}>
                Popular Tags
              </Typography>
              
              <Grid container spacing={1}>
                {stats.mostFrequentTags.slice(0, 5).map((tag) => (
                  <Grid item key={tag.tag}>
                    <Chip 
                      label={`${tag.tag}: ${tag.count}`}
                      size="small"
                    />
                  </Grid>
                ))}
              </Grid>
              
              {/* Trends summary */}
              <Box sx={{ mt: 3 }}>
                <Typography variant="subtitle1" gutterBottom>
                  Recent Trends
                </Typography>
                
                <Box sx={{ display: 'flex', alignItems: 'center' }}>
                  {stats.trendsData[stats.trendsData.length - 1].successCount > 
                   stats.trendsData[stats.trendsData.length - 2].successCount ? (
                    <TrendingUpIcon color="success" sx={{ mr: 1 }} />
                  ) : (
                    <TrendingDownIcon color="error" sx={{ mr: 1 }} />
                  )}
                  
                  <Typography variant="body2">
                    {stats.trendsData[stats.trendsData.length - 1].successCount > 
                     stats.trendsData[stats.trendsData.length - 2].successCount ? (
                      `Success rate increased by ${stats.trendsData[stats.trendsData.length - 1].successCount - 
                       stats.trendsData[stats.trendsData.length - 2].successCount} jobs compared to yesterday`
                    ) : (
                      `Success rate decreased by ${stats.trendsData[stats.trendsData.length - 2].successCount - 
                       stats.trendsData[stats.trendsData.length - 1].successCount} jobs compared to yesterday`
                    )}
                  </Typography>
                </Box>
              </Box>
            </Box>
          </Box>
        ) : (
          // No data available
          <Box sx={{ textAlign: 'center', py: 4 }}>
            <InfoIcon color="action" sx={{ fontSize: 40, mb: 2 }} />
            <Typography>No statistics available</Typography>
            <Button
              variant="outlined"
              startIcon={<RefreshIcon />}
              onClick={handleRefresh}
              sx={{ mt: 2 }}
              disabled={loading}
            >
              Load Statistics
            </Button>
          </Box>
        )}
      </CardContent>
    </Card>
  );
};

export default JobStatsCard;