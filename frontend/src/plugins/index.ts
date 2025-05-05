/**
 * Plugin system for Deployment Portal
 * 
 * This module provides a flexible plugin architecture that supports:
 * - Plugin registration
 * - Lazy loading of plugins
 * - Plugin lifecycle hooks
 * - Plugin configuration
 */

// Plugin interface
export interface Plugin {
  id: string;
  name: string;
  version: string;
  description?: string;
  
  // Required methods
  initialize: (options?: any) => Promise<void> | void;
  
  // Optional methods
  onLoad?: () => Promise<void> | void;
  onUnload?: () => Promise<void> | void;
  
  // Plugin can have any additional properties
  [key: string]: any;
}

// Plugin Registration Options
export interface PluginRegistrationOptions {
  autoInitialize?: boolean;
  config?: Record<string, any>;
}

// Plugin Manager 
class PluginManager {
  private plugins: Map<string, Plugin> = new Map();
  private initializedPlugins: Set<string> = new Set();
  private pluginImports: Map<string, () => Promise<{ default: Plugin }>> = new Map();
  
  /**
   * Register a plugin
   * @param plugin Plugin instance or lazy import function
   * @param options Registration options
   */
  register(
    plugin: Plugin | (() => Promise<{ default: Plugin }>),
    options: PluginRegistrationOptions = {}
  ): void {
    if (typeof plugin === 'function') {
      // This is a lazy-loaded plugin
      this.registerLazyPlugin(plugin, options);
    } else {
      // This is a directly provided plugin instance
      this.registerDirectPlugin(plugin, options);
    }
  }

  /**
   * Register a plugin that is directly provided
   */
  private registerDirectPlugin(
    plugin: Plugin,
    options: PluginRegistrationOptions = {}
  ): void {
    if (this.plugins.has(plugin.id)) {
      console.warn(`Plugin ${plugin.id} is already registered. Skipping.`);
      return;
    }

    this.plugins.set(plugin.id, plugin);
    console.log(`Plugin ${plugin.id} (${plugin.name}) registered.`);

    if (options.autoInitialize) {
      this.initializePlugin(plugin.id, options.config);
    }
  }

  /**
   * Register a plugin that will be lazy-loaded
   */
  private registerLazyPlugin(
    importFn: () => Promise<{ default: Plugin }>,
    options: PluginRegistrationOptions = {}
  ): void {
    // We don't know the plugin ID yet, so generate a temporary ID
    const tempId = `lazy-plugin-${this.pluginImports.size}`;
    this.pluginImports.set(tempId, importFn);
    
    console.log(`Lazy plugin registered with temporary ID: ${tempId}`);
    
    // If autoInitialize is set, we need to immediately load and initialize the plugin
    if (options.autoInitialize) {
      this.loadPlugin(tempId)
        .then(plugin => {
          if (plugin) {
            this.initializePlugin(plugin.id, options.config);
          }
        });
    }
  }

  /**
   * Load a lazy-loaded plugin by its temporary ID
   */
  private async loadPlugin(tempId: string): Promise<Plugin | null> {
    const importFn = this.pluginImports.get(tempId);
    if (!importFn) {
      console.error(`No lazy plugin found with temporary ID: ${tempId}`);
      return null;
    }

    try {
      const { default: plugin } = await importFn();
      
      // Remove the temporary import function
      this.pluginImports.delete(tempId);
      
      // Register the actual plugin
      this.plugins.set(plugin.id, plugin);
      
      console.log(`Lazy plugin ${plugin.id} (${plugin.name}) loaded.`);
      
      // Call onLoad hook if available
      if (plugin.onLoad) {
        await plugin.onLoad();
      }
      
      return plugin;
    } catch (error) {
      console.error(`Failed to load lazy plugin with temporary ID: ${tempId}`, error);
      return null;
    }
  }

  /**
   * Initialize a plugin by ID
   * @param pluginId Plugin ID
   * @param config Optional configuration
   */
  async initializePlugin(
    pluginId: string, 
    config?: Record<string, any>
  ): Promise<boolean> {
    // Check if this is a temporary ID for a lazy plugin
    if (this.pluginImports.has(pluginId)) {
      const plugin = await this.loadPlugin(pluginId);
      if (!plugin) return false;
      
      // Use the real plugin ID now
      pluginId = plugin.id;
    }
    
    const plugin = this.plugins.get(pluginId);
    
    if (!plugin) {
      console.error(`Plugin ${pluginId} not found.`);
      return false;
    }
    
    if (this.initializedPlugins.has(pluginId)) {
      console.warn(`Plugin ${pluginId} is already initialized.`);
      return true;
    }
    
    try {
      await plugin.initialize(config);
      this.initializedPlugins.add(pluginId);
      console.log(`Plugin ${pluginId} (${plugin.name}) initialized.`);
      return true;
    } catch (error) {
      console.error(`Failed to initialize plugin ${pluginId}:`, error);
      return false;
    }
  }

  /**
   * Unload a plugin by ID
   * @param pluginId Plugin ID
   */
  async unloadPlugin(pluginId: string): Promise<boolean> {
    const plugin = this.plugins.get(pluginId);
    
    if (!plugin) {
      console.error(`Plugin ${pluginId} not found.`);
      return false;
    }
    
    if (!this.initializedPlugins.has(pluginId)) {
      console.warn(`Plugin ${pluginId} is not initialized.`);
      // Still remove it from the plugins list
      this.plugins.delete(pluginId);
      return true;
    }
    
    try {
      if (plugin.onUnload) {
        await plugin.onUnload();
      }
      
      this.initializedPlugins.delete(pluginId);
      this.plugins.delete(pluginId);
      console.log(`Plugin ${pluginId} (${plugin.name}) unloaded.`);
      return true;
    } catch (error) {
      console.error(`Failed to unload plugin ${pluginId}:`, error);
      return false;
    }
  }

  /**
   * Get a plugin by ID
   * @param pluginId Plugin ID
   */
  getPlugin<T extends Plugin = Plugin>(pluginId: string): T | null {
    return (this.plugins.get(pluginId) as T) || null;
  }

  /**
   * Check if a plugin is registered
   * @param pluginId Plugin ID
   */
  hasPlugin(pluginId: string): boolean {
    return this.plugins.has(pluginId);
  }

  /**
   * Check if a plugin is initialized
   * @param pluginId Plugin ID
   */
  isPluginInitialized(pluginId: string): boolean {
    return this.initializedPlugins.has(pluginId);
  }

  /**
   * Get all registered plugins
   */
  getAllPlugins(): Plugin[] {
    return Array.from(this.plugins.values());
  }

  /**
   * Get all initialized plugins
   */
  getInitializedPlugins(): Plugin[] {
    return this.getAllPlugins().filter(plugin => 
      this.initializedPlugins.has(plugin.id)
    );
  }
}

// Create and export a singleton instance
export const pluginManager = new PluginManager();

// Export a convenience function for registering plugins
export const registerPlugin = (
  plugin: Plugin | (() => Promise<{ default: Plugin }>),
  options?: PluginRegistrationOptions
): void => {
  pluginManager.register(plugin, options);
};

// Export utility for lazy loading plugins
export const lazyLoadPlugin = async (pluginId: string): Promise<Plugin | null> => {
  // If plugin is already loaded, return it
  if (pluginManager.hasPlugin(pluginId)) {
    return pluginManager.getPlugin(pluginId);
  }

  // Otherwise, we need to try to find a matching lazy plugin import
  // This implementation is simplified - in a real-world scenario,
  // you might need a more robust way to map plugin IDs to their lazy imports
  
  // For now, just try to dynamically import based on plugin ID
  try {
    // Dynamic import of the plugin
    const module = await import(`./${pluginId}`);
    const plugin = module.default;
    
    // Register the plugin
    pluginManager.register(plugin, { autoInitialize: true });
    
    return plugin;
  } catch (error) {
    console.error(`Failed to lazy load plugin ${pluginId}:`, error);
    return null;
  }
};

// A helper function to create a type-safe plugin wrapper
export function createPlugin<T extends Omit<Plugin, 'id' | 'name' | 'version'>>(
  id: string,
  name: string,
  version: string,
  implementation: T
): Plugin {
  return {
    id,
    name,
    version,
    ...implementation,
  };
}

export default pluginManager;