import { createPlugin } from './index';

// Interface for notification configuration
interface NotificationConfig {
  maxNotifications?: number;
  autoCleanup?: boolean;
  pollingInterval?: number;
}

// Notification data structure
interface Notification {
  id: string;
  title: string;
  message: string;
  type: 'info' | 'success' | 'warning' | 'error';
  timestamp: Date;
  read: boolean;
}

// Notification Plugin Implementation
const notificationPlugin = createPlugin(
  'notifications',
  'Notifications Plugin',
  '1.0.0',
  {
    description: 'Provides notification management functionality',
    
    // Plugin state
    notifications: [] as Notification[],
    config: {
      maxNotifications: 100,
      autoCleanup: true,
      pollingInterval: 60000, // 1 minute
    } as NotificationConfig,
    pollingTimer: undefined as NodeJS.Timeout | undefined,
    
    // Implementation of required initialize method
    async initialize(options?: NotificationConfig): Promise<void> {
      console.log('Initializing Notifications Plugin...');
      
      // Merge provided options with default config
      if (options) {
        this.config = { ...this.config, ...options };
      }
      
      // Start polling for new notifications
      this.startPolling();
      
      console.log('Notifications Plugin initialized with config:', this.config);
    },
    
    // Optional lifecycle hooks
    onLoad(): void {
      console.log('Notifications Plugin loaded');
    },
    
    onUnload(): void {
      console.log('Notifications Plugin unloaded');
      
      // Clean up
      if (this.pollingTimer) {
        clearInterval(this.pollingTimer);
      }
    },
    
    // Plugin-specific methods
    startPolling(): void {
      if (this.pollingTimer) {
        clearInterval(this.pollingTimer);
      }
      
      this.pollingTimer = setInterval(() => {
        this.checkForNewNotifications();
      }, this.config.pollingInterval);
      
      console.log(`Notification polling started (interval: ${this.config.pollingInterval}ms)`);
    },
    
    async checkForNewNotifications(): Promise<void> {
      try {
        // This would typically be an API call
        // For demo purposes, randomly create a notification
        if (Math.random() > 0.7) {
          const types = ['info', 'success', 'warning', 'error'] as const;
          const randomType = types[Math.floor(Math.random() * types.length)];
          
          this.addNotification({
            title: `Test ${randomType} notification`,
            message: `This is a test ${randomType} notification generated at ${new Date().toLocaleTimeString()}`,
            type: randomType,
          });
        }
        
        // Clean up old notifications if configured
        if (this.config.autoCleanup && this.notifications.length > this.config.maxNotifications) {
          this.cleanupNotifications();
        }
      } catch (error) {
        console.error('Failed to check for new notifications:', error);
      }
    },
    
    addNotification({ 
      title, 
      message, 
      type = 'info' 
    }: { 
      title: string; 
      message: string; 
      type?: 'info' | 'success' | 'warning' | 'error'; 
    }): Notification {
      const notification: Notification = {
        id: Date.now().toString(36) + Math.random().toString(36).substr(2),
        title,
        message,
        type,
        timestamp: new Date(),
        read: false,
      };
      
      this.notifications.unshift(notification);
      
      // Dispatch an event that can be listened to by the UI
      window.dispatchEvent(
        new CustomEvent('notification:new', { detail: notification })
      );
      
      console.log(`New notification added: ${notification.title}`);
      
      return notification;
    },
    
    markAsRead(notificationId: string): void {
      const notification = this.notifications.find(n => n.id === notificationId);
      
      if (notification) {
        notification.read = true;
        
        // Dispatch an event that can be listened to by the UI
        window.dispatchEvent(
          new CustomEvent('notification:updated', { detail: notification })
        );
        
        console.log(`Notification ${notificationId} marked as read`);
      }
    },
    
    deleteNotification(notificationId: string): void {
      const index = this.notifications.findIndex(n => n.id === notificationId);
      
      if (index !== -1) {
        const [notification] = this.notifications.splice(index, 1);
        
        // Dispatch an event that can be listened to by the UI
        window.dispatchEvent(
          new CustomEvent('notification:deleted', { detail: notification })
        );
        
        console.log(`Notification ${notificationId} deleted`);
      }
    },
    
    cleanupNotifications(): void {
      // Keep the most recent notifications up to maxNotifications
      if (this.notifications.length > this.config.maxNotifications) {
        const excessCount = this.notifications.length - this.config.maxNotifications;
        
        // Sort by read status (keep unread) then by timestamp (keep newer)
        this.notifications.sort((a, b) => {
          if (a.read !== b.read) {
            return a.read ? 1 : -1; // Put unread first
          }
          return b.timestamp.getTime() - a.timestamp.getTime(); // Then sort by timestamp (newer first)
        });
        
        const notificationsToRemove = this.notifications.splice(
          this.config.maxNotifications,
          excessCount
        );
        
        console.log(`Cleaned up ${notificationsToRemove.length} old notifications`);
      }
    },
    
    getAllNotifications(): Notification[] {
      return [...this.notifications];
    },
    
    getUnreadNotifications(): Notification[] {
      return this.notifications.filter(n => !n.read);
    },
  }
);

export default notificationPlugin;