using System.Text;
using JobTriggerPlatform.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace SamplePlugin;

/// <summary>
/// A sample implementation of a job trigger plugin.
/// </summary>
public class SampleJobTriggerPlugin : IJobTriggerPlugin
{
    private readonly ILogger<SampleJobTriggerPlugin>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SampleJobTriggerPlugin"/> class.
    /// </summary>
    /// <param name="logger">The logger (optional).</param>
    public SampleJobTriggerPlugin(ILogger<SampleJobTriggerPlugin>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public string JobName => "SampleJob";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> RequiredRoles => new[] { "Admin", "Dev" };

    /// <inheritdoc/>
    public IReadOnlyCollection<PluginParameter> Parameters => new List<PluginParameter>
    {
        new PluginParameter
        {
            Name = "environment",
            DisplayName = "Environment",
            Description = "The environment to deploy to",
            Type = ParameterType.Select,
            PossibleValues = new[] { "Development", "Staging", "Production" }
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
            Name = "skipTests",
            DisplayName = "Skip Tests",
            Description = "Whether to skip tests during deployment",
            Type = ParameterType.Boolean,
            DefaultValue = "false"
        }
    };

    /// <inheritdoc/>
    public async Task<PluginResult> TriggerAsync(IDictionary<string, string> parameters, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Triggering sample job with parameters: {@Parameters}", parameters);

        // Simulate a long-running operation
        await Task.Delay(2000, cancellationToken);

        // Extract parameters
        var environment = parameters.TryGetValue("environment", out var env) ? env : "Development";
        var version = parameters.TryGetValue("version", out var ver) ? ver : "1.0.0";
        var skipTests = parameters.TryGetValue("skipTests", out var skip) && bool.TryParse(skip, out var skipResult) && skipResult;

        // Build logs
        var logs = new List<string>
        {
            $"Starting deployment of version {version} to {environment}",
            skipTests ? "Tests will be skipped" : "Running tests...",
        };

        if (!skipTests)
        {
            logs.Add("Tests passed successfully");
        }

        logs.Add($"Deploying version {version} to {environment}");
        logs.Add("Deployment completed successfully");

        // Return result
        return PluginResult.Success(
            data: new
            {
                Environment = environment,
                Version = version,
                DeploymentTime = DateTime.UtcNow,
                SkippedTests = skipTests
            },
            details: $"Successfully deployed version {version} to {environment}",
            logs: logs);
    }
}
