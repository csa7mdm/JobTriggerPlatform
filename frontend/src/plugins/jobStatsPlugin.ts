import { createPlugin } from './index';
import axios from 'axios';

// Define plugin-specific types
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

interface JobStatsConfig {
  cacheTimeout: number;
  analyticsEnabled: boolean;
  autoRefresh: boolean;
  refreshInterval: number;
}

// Create the Job Stats Plugin
const jobStatsPlugin = createPlugin(
  'job-stats',
  'Job Statistics',
  '1.0.0',
  {
    description: 'Provides advanced job statistics and analytics',
    
    // Plugin state
    stats: null as JobStats | null,
    lastUpdated: null as Date | null,
    refreshTimer: null as NodeJS.Timeout | null,
    isLoading: false,
    error: null as string | null,
    
    // Default configuration
    config: {
      cacheTimeout: 300000, // 5 minutes
      analyticsEnabled: true,
      autoRefresh: true,
      refreshInterval: 300000, // 5 minutes
    } as JobStatsConfig,
    
    // Required initialize method
    async initialize(options?: Partial<JobStatsConfig>): Promise<void> {
      console.log('Initializing Job Stats Plugin...');
      
      // Merge provided options with default config
      if (options) {
        this.config = { ...this.config, ...options };
      }
      
      // Load initial stats
      try {
        await this.loadStats();
      } catch (error) {
        console.error('Failed to load initial job stats:', error);
        this.error = 'Failed to load initial statistics';
      }
      
      // Start auto-refresh if enabled
      if (this.config.autoRefresh) {
        this.startAutoRefresh();
      }
      
      console.log('Job Stats Plugin initialized');
    },
    
    // Optional lifecycle hooks
    onUnload(): void {
      console.log('Job Stats Plugin unloaded');
      
      // Clear the refresh timer if it exists
      if (this.refreshTimer) {
        clearInterval(this.refreshTimer);
        this.refreshTimer = null;
      }
    },
    
    // Plugin-specific methods
    async loadStats(): Promise<JobStats> {
      // Set loading state
      this.isLoading = true;
      this.error = null;
      
      try {
        // Check if we have cached stats that are still valid
        if (
          this.stats && 
          this.lastUpdated && 
          Date.now() - this.lastUpdated.getTime() < this.config.cacheTimeout
        ) {
          console.log('Using cached job stats');
          this.isLoading = false;
          return this.stats;
        }
        
        // Fetch job statistics from the API
        const response = await axios.get('/api/analytics/job-stats');
        
        // Update the plugin state
        this.stats = response.data;
        this.lastUpdated = new Date();
        
        // Dispatch an event that can be listened to by the UI
        window.dispatchEvent(
          new CustomEvent('job-stats:updated', { detail: this.stats })
        );
        
        console.log('Job stats loaded successfully');
        return this.stats;
      } catch (error) {
        console.error('Failed to load job stats:', error);
        this.error = 'Failed to load statistics from server';
        
        // Dispatch an error event
        window.dispatchEvent(
          new CustomEvent('job-stats:error', { detail: this.error })
        );
        
        throw error;
      } finally {
        this.isLoading = false;
      }
    },
    
    startAutoRefresh(): void {
      // Clear any existing timer
      if (this.refreshTimer) {
        clearInterval(this.refreshTimer);
      }
      
      // Set up a new timer
      this.refreshTimer = setInterval(() => {
        console.log('Auto-refreshing job stats...');
        this.loadStats().catch(error => {
          console.error('Auto-refresh failed:', error);
        });
      }, this.config.refreshInterval);
      
      console.log(`Auto-refresh started with interval: ${this.config.refreshInterval}ms`);
    },
    
    stopAutoRefresh(): void {
      if (this.refreshTimer) {
        clearInterval(this.refreshTimer);
        this.refreshTimer = null;
        console.log('Auto-refresh stopped');
      }
    },
    
    getStats(): JobStats | null {
      return this.stats;
    },
    
    async getStatsForEnvironment(environment: string): Promise<Partial<JobStats> | null> {
      // Ensure we have stats loaded
      if (!this.stats) {
        await this.loadStats();
      }
      
      if (!this.stats) {
        return null;
      }
      
      // In a real implementation, this would make a more specific API call
      // For demo purposes, we'll filter the existing stats
      
      return {
        totalJobs: this.stats.jobsByEnvironment[environment] || 0,
        // Other environment-specific stats would be added here
      };
    },
    
    async getTagStats(tag: string): Promise<Array<{ date: string; count: number }> | null> {
      try {
        // This would typically be a specific API call
        const response = await axios.get(`/api/analytics/tag-stats/${encodeURIComponent(tag)}`);
        return response.data;
      } catch (error) {
        console.error(`Failed to get stats for tag ${tag}:`, error);
        return null;
      }
    },
    
    isStatsLoading(): boolean {
      return this.isLoading;
    },
    
    getLastError(): string | null {
      return this.error;
    },
    
    getLastUpdated(): Date | null {
      return this.lastUpdated;
    },
  }
);

export default jobStatsPlugin;