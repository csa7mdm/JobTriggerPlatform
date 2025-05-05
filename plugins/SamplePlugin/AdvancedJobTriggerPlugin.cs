using JobTriggerPlatform.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace SamplePlugin;

/// <summary>
/// An advanced sample implementation of a job trigger plugin
/// that demonstrates custom role requirements.
/// </summary>
public class AdvancedJobTriggerPlugin : IJobTriggerPlugin
{
    private readonly ILogger<AdvancedJobTriggerPlugin>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdvancedJobTriggerPlugin"/> class.
    /// </summary>
    /// <param name="logger">The logger (optional).</param>
    public AdvancedJobTriggerPlugin(ILogger<AdvancedJobTriggerPlugin>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public string JobName => "AdvancedDeployment";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> RequiredRoles => new[] { "Admin" };

    /// <inheritdoc/>
    public IReadOnlyCollection<PluginParameter> Parameters => new List<PluginParameter>
    {
        new PluginParameter
        {
            Name = "environment",
            DisplayName = "Environment",
            Description = "The environment to deploy to",
            Type = ParameterType.Select,
            PossibleValues = new[] { "QA", "UAT", "Production" }
        },
        new PluginParameter
        {
            Name = "version",
            DisplayName = "Version",
            Description = "The version to deploy",
            Type = ParameterType.String,
            IsRequired = true
        },
        new PluginParameter
        {
            Name = "notifyUsers",
            DisplayName = "Notify Users",
            Description = "Whether to notify users about the deployment",
            Type = ParameterType.Boolean,
            DefaultValue = "true"
        },
        new PluginParameter
        {
            Name = "deploymentScript",
            DisplayName = "Deployment Script",
            Description = "Custom deployment script to run",
            Type = ParameterType.File,
            IsRequired = false
        }
    };

    /// <inheritdoc/>
    public async Task<PluginResult> TriggerAsync(IDictionary<string, string> parameters, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Triggering advanced deployment with parameters: {@Parameters}", parameters);

        // Simulate a long-running operation
        await Task.Delay(3000, cancellationToken);

        // Extract parameters
        var environment = parameters.TryGetValue("environment", out var env) ? env : "QA";
        var version = parameters.TryGetValue("version", out var ver) ? ver : "1.0.0";
        var notifyUsers = parameters.TryGetValue("notifyUsers", out var notify) && bool.TryParse(notify, out var notifyResult) && notifyResult;
        var deploymentScript = parameters.TryGetValue("deploymentScript", out var script) ? script : null;

        // Build logs
        var logs = new List<string>
        {
            $"Starting advanced deployment of version {version} to {environment}",
            "Validating deployment parameters...",
            "Parameters validated successfully",
            "Checking dependencies...",
            "All dependencies available",
            "Backing up current version...",
            "Backup completed successfully",
        };

        if (!string.IsNullOrEmpty(deploymentScript))
        {
            logs.Add("Using custom deployment script");
            logs.Add($"Executing script: {deploymentScript}");
        }
        else
        {
            logs.Add("Using standard deployment process");
        }

        logs.Add($"Deploying version {version} to {environment}");
        logs.Add("Running database migrations...");
        logs.Add("Database migrations completed successfully");
        logs.Add("Restarting services...");
        logs.Add("Services restarted successfully");
        
        if (notifyUsers)
        {
            logs.Add("Sending notifications to users");
            logs.Add("User notifications sent successfully");
        }

        logs.Add($"Advanced deployment to {environment} completed successfully");

        // Return result
        return PluginResult.Success(
            data: new
            {
                Environment = environment,
                Version = version,
                DeploymentTime = DateTime.UtcNow,
                NotifiedUsers = notifyUsers,
                UsedCustomScript = !string.IsNullOrEmpty(deploymentScript)
            },
            details: $"Successfully deployed version {version} to {environment}",
            logs: logs);
    }
}
