import { createPlugin } from './index';

interface ThemeConfig {
  defaultTheme?: 'light' | 'dark' | 'system';
  enableUserPreference?: boolean;
  customColors?: Record<string, string>;
}

const themePlugin = createPlugin(
  'theme',
  'Theme Manager',
  '1.0.0',
  {
    description: 'Manages application themes and color schemes',
    
    // Plugin state
    currentTheme: 'light' as 'light' | 'dark' | 'system',
    systemTheme: 'light' as 'light' | 'dark',
    config: {
      defaultTheme: 'system',
      enableUserPreference: true,
      customColors: {}
    } as ThemeConfig,
    
    // Required initialize method
    async initialize(options?: ThemeConfig): Promise<void> {
      console.log('Initializing Theme Plugin...');
      
      // Merge provided options with default config
      if (options) {
        this.config = { ...this.config, ...options };
      }
      
      // Set initial theme
      this.currentTheme = this.loadSavedTheme() || this.config.defaultTheme || 'system';
      
      // Detect system theme
      this.systemTheme = this.detectSystemTheme();
      
      // Apply the theme
      this.applyTheme();
      
      // Add event listeners
      if (this.currentTheme === 'system') {
        this.addSystemThemeListener();
      }
      
      console.log(`Theme Plugin initialized with theme: ${this.currentTheme}`);
    },
    
    // Optional lifecycle hooks
    onLoad(): void {
      console.log('Theme Plugin loaded');
    },
    
    onUnload(): void {
      console.log('Theme Plugin unloaded');
      this.removeSystemThemeListener();
    },
    
    // Plugin-specific methods
    loadSavedTheme(): 'light' | 'dark' | 'system' | null {
      if (!this.config.enableUserPreference) {
        return null;
      }
      
      try {
        const saved = localStorage.getItem('theme-preference');
        if (saved && ['light', 'dark', 'system'].includes(saved)) {
          return saved as 'light' | 'dark' | 'system';
        }
      } catch (error) {
        console.error('Failed to load theme preference:', error);
      }
      
      return null;
    },
    
    saveTheme(theme: 'light' | 'dark' | 'system'): void {
      if (!this.config.enableUserPreference) {
        return;
      }
      
      try {
        localStorage.setItem('theme-preference', theme);
      } catch (error) {
        console.error('Failed to save theme preference:', error);
      }
    },
    
    detectSystemTheme(): 'light' | 'dark' {
      return window.matchMedia('(prefers-color-scheme: dark)').matches
        ? 'dark'
        : 'light';
    },
    
    addSystemThemeListener(): void {
      window.matchMedia('(prefers-color-scheme: dark)')
        .addEventListener('change', this.handleSystemThemeChange.bind(this));
    },
    
    removeSystemThemeListener(): void {
      window.matchMedia('(prefers-color-scheme: dark)')
        .removeEventListener('change', this.handleSystemThemeChange.bind(this));
    },
    
    handleSystemThemeChange(event: MediaQueryListEvent): void {
      this.systemTheme = event.matches ? 'dark' : 'light';
      
      if (this.currentTheme === 'system') {
        this.applyTheme();
      }
    },
    
    applyTheme(): void {
      // Determine the actual theme to apply
      const themeToApply = this.currentTheme === 'system'
        ? this.systemTheme
        : this.currentTheme;
      
      // Apply the theme to the document
      document.documentElement.setAttribute('data-theme', themeToApply);
      
      // Apply custom colors if any
      Object.entries(this.config.customColors || {}).forEach(([key, value]) => {
        document.documentElement.style.setProperty(`--color-${key}`, value);
      });
      
      // Dispatch an event that can be listened to by the UI
      window.dispatchEvent(
        new CustomEvent('theme:changed', { 
          detail: { 
            theme: themeToApply,
            effectiveTheme: themeToApply,
            systemTheme: this.systemTheme
          } 
        })
      );
      
      console.log(`Applied theme: ${themeToApply}`);
    },
    
    setTheme(theme: 'light' | 'dark' | 'system'): void {
      if (this.currentTheme === theme) {
        return;
      }
      
      // Update current theme
      this.currentTheme = theme;
      
      // Save preference
      this.saveTheme(theme);
      
      // Add or remove system theme listener
      if (theme === 'system') {
        this.addSystemThemeListener();
      } else {
        this.removeSystemThemeListener();
      }
      
      // Apply the theme
      this.applyTheme();
    },
    
    getTheme(): 'light' | 'dark' | 'system' {
      return this.currentTheme;
    },
    
    getEffectiveTheme(): 'light' | 'dark' {
      return this.currentTheme === 'system'
        ? this.systemTheme
        : this.currentTheme;
    },
    
    toggleTheme(): void {
      const currentEffectiveTheme = this.getEffectiveTheme();
      const newTheme = currentEffectiveTheme === 'light' ? 'dark' : 'light';
      this.setTheme(newTheme);
    },
  }
);

export default themePlugin;