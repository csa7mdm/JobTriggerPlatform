# Plugin System

The Deployment Portal includes a flexible plugin system that allows extending the application's functionality with modular, dynamically loaded components.

## Features

- Plugin registration and management
- Lazy loading support via dynamic imports
- Plugin lifecycle hooks
- Type-safe plugin creation helpers
- Configuration support

## Usage

### Creating a Plugin

To create a plugin, use the `createPlugin` helper:

```typescript
import { createPlugin } from './plugins';

const myPlugin = createPlugin(
  'unique-plugin-id',
  'My Plugin',
  '1.0.0',
  {
    description: 'A description of what my plugin does',
    
    // Required initialize method
    async initialize(options?: any): Promise<void> {
      console.log('Plugin initialized with options:', options);
      // Your initialization code here
    },
    
    // Optional lifecycle hooks
    onLoad(): void {
      console.log('Plugin loaded');
    },
    
    onUnload(): void {
      console.log('Plugin unloaded, cleaning up resources...');
    },
    
    // Custom methods and properties
    customMethod(): void {
      console.log('This is a custom method');
    },
    
    someProperty: 'value'
  }
);

export default myPlugin;
```

### Registering a Plugin

#### Direct Registration

Register a plugin directly when it's imported:

```typescript
import { registerPlugin } from './plugins';
import myPlugin from './plugins/myPlugin';

// Register with default options
registerPlugin(myPlugin);

// Or register with options
registerPlugin(myPlugin, {
  autoInitialize: true,
  config: {
    // Plugin-specific configuration
    setting1: 'value1',
    setting2: true
  }
});
```

#### Lazy Loading Registration

Register a plugin that will be loaded only when needed:

```typescript
import { registerPlugin } from './plugins';

// Register with a dynamic import function
registerPlugin(
  () => import('./plugins/heavyPlugin'),
  {
    autoInitialize: false
  }
);
```

### Accessing Plugins

```typescript
import { pluginManager } from './plugins';

// Get a plugin by ID
const myPlugin = pluginManager.getPlugin('my-plugin-id');

// Use plugin methods
if (myPlugin) {
  myPlugin.customMethod();
}

// Type-safe access
interface MyPluginType extends Plugin {
  customMethod: () => void;
  someProperty: string;
}

const typedPlugin = pluginManager.getPlugin<MyPluginType>('my-plugin-id');
if (typedPlugin) {
  console.log(typedPlugin.someProperty);
}
```

### Lazy Loading a Plugin On Demand

```typescript
import { lazyLoadPlugin } from './plugins';

async function loadWhenNeeded() {
  const plugin = await lazyLoadPlugin('feature-plugin');
  
  if (plugin) {
    // Use the plugin
    console.log(`Loaded ${plugin.name} v${plugin.version}`);
  }
}
```

## Plugin Lifecycle

Plugins go through the following lifecycle:

1. **Registration**: The plugin is registered with the plugin manager
2. **Loading**: If lazy-loaded, the plugin is loaded when needed
3. **Initialization**: The plugin's `initialize()` method is called
4. **Usage**: The plugin is used by the application
5. **Unloading**: When no longer needed, the plugin's `onUnload()` method is called

## Examples

### Notification Plugin

The Notifications Plugin (`notificationPlugin.ts`) provides notification management:

```typescript
// Get the notifications plugin
const notificationsPlugin = pluginManager.getPlugin('notifications');

// Add a notification
notificationsPlugin.addNotification({
  title: 'Job completed',
  message: 'Deployment job has completed successfully',
  type: 'success'
});

// Get unread notifications
const unread = notificationsPlugin.getUnreadNotifications();
```

### Theme Plugin

The Theme Plugin (`themePlugin.ts`) manages application themes:

```typescript
// Get the theme plugin
const themePlugin = pluginManager.getPlugin('theme');

// Toggle between light and dark mode
themePlugin.toggleTheme();

// Set a specific theme
themePlugin.setTheme('dark');

// Get current theme information
const currentTheme = themePlugin.getTheme(); // 'light', 'dark', or 'system'
const effectiveTheme = themePlugin.getEffectiveTheme(); // 'light' or 'dark'
```

## Best Practices

1. **Unique IDs**: Ensure each plugin has a unique ID to avoid conflicts
2. **Lazy Loading**: Use lazy loading for plugins with heavy dependencies or that aren't used immediately
3. **Cleanup**: Properly clean up resources in the `onUnload` method
4. **Type Safety**: Use TypeScript interfaces to ensure type safety when accessing plugins
5. **Event-Based Communication**: Use custom events for plugins to communicate with each other
6. **Configuration**: Design plugins to be configurable for flexibility
