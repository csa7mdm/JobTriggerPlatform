using System.Reflection;
using System.Runtime.Loader;
using JobTriggerPlatform.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobTriggerPlatform.Infrastructure.Plugins;

/// <summary>
/// Handles the loading of job trigger plugins from external assemblies.
/// </summary>
public class PluginLoader
{
    private readonly ILogger<PluginLoader> _logger;
    private readonly string _pluginsPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLoader"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="pluginsPath">The path to the plugins directory. Defaults to "Plugins" relative to the current directory.</param>
    public PluginLoader(ILogger<PluginLoader> logger, string? pluginsPath = null)
    {
        _logger = logger;
        _pluginsPath = pluginsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
    }

    /// <summary>
    /// Loads all plugins from the plugins directory and registers them with the specified service collection.
    /// </summary>
    /// <param name="services">The service collection to register plugins with.</param>
    public void LoadPlugins(IServiceCollection services)
    {
        _logger.LogInformation("Loading plugins from {PluginsPath}", _pluginsPath);

        if (!Directory.Exists(_pluginsPath))
        {
            _logger.LogWarning("Plugins directory {PluginsPath} does not exist", _pluginsPath);
            return;
        }

        foreach (var pluginDll in Directory.GetFiles(_pluginsPath, "*.dll"))
        {
            try
            {
                LoadPlugin(services, pluginDll);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from {PluginDll}", pluginDll);
            }
        }
    }

    private void LoadPlugin(IServiceCollection services, string pluginPath)
    {
        _logger.LogDebug("Loading plugin from {PluginPath}", pluginPath);

        // Create a new assembly load context for the plugin
        var loadContext = new PluginLoadContext(pluginPath);
        
        try
        {
            // Load the assembly
            var assembly = loadContext.LoadFromAssemblyPath(pluginPath);
            _logger.LogDebug("Loaded assembly {AssemblyName}", assembly.FullName);

            // Find all types that implement IJobTriggerPlugin
            var pluginTypes = assembly.GetTypes()
                .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IJobTriggerPlugin).IsAssignableFrom(type))
                .ToList();

            if (pluginTypes.Count == 0)
            {
                _logger.LogWarning("No plugin types found in {PluginPath}", pluginPath);
                return;
            }

            // Register each plugin type
            foreach (var pluginType in pluginTypes)
            {
                _logger.LogInformation("Registering plugin {PluginType}", pluginType.FullName);
                services.AddSingleton(typeof(IJobTriggerPlugin), pluginType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load or register plugins from {PluginPath}", pluginPath);
            throw;
        }
    }
}

/// <summary>
/// A custom assembly load context for loading plugin assemblies.
/// </summary>
internal class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLoadContext"/> class.
    /// </summary>
    /// <param name="pluginPath">The path to the plugin assembly.</param>
    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    /// <inheritdoc/>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}

/// <summary>
/// Extension methods for adding plugins to the service collection.
/// </summary>
public static class PluginServiceCollectionExtensions
{
    /// <summary>
    /// Adds job trigger plugins to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="pluginsPath">Optional custom path to the plugins directory.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPlugins(this IServiceCollection services, string? pluginsPath = null)
    {
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<PluginLoader>>();
        
        var pluginLoader = new PluginLoader(logger, pluginsPath);
        pluginLoader.LoadPlugins(services);
        
        return services;
    }
}
