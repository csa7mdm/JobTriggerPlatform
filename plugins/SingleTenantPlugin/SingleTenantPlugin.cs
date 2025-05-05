using JobTriggerPlatform.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace SingleTenantPlugin;

/// <summary>
/// Plugin for deploying single-tenant applications to specified environments.
/// </summary>
public class SingleTenantPlugin : IJobTriggerPlugin
{
    private readonly IJenkinsService _jenkinsService;
    private readonly ILogger<SingleTenantPlugin> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleTenantPlugin"/> class.
    /// </summary>
    /// <param name="jenkinsService">The Jenkins service.</param>
    /// <param name="logger">The logger.</param>
    public SingleTenantPlugin(IJenkinsService jenkinsService, ILogger<SingleTenantPlugin> logger)
    {
        _jenkinsService = jenkinsService ?? throw new ArgumentNullException(nameof(jenkinsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public string JobName => "SingleTenantDeployment";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> RequiredRoles => new[] { "Admin", "Dev" };

    /// <inheritdoc/>
    public IReadOnlyCollection<PluginParameter> Parameters => new List<PluginParameter>
    {
        // Product selection
        new PluginParameter
        {
            Name = "product",
            DisplayName = "Product",
            Description = "The product to deploy",
            Type = ParameterType.Select,
            IsRequired = true,
            PossibleValues = new[] { "WebApp", "API", "UserService", "PaymentService", "NotificationService" }
        },
        
        // Environment selection
        new PluginParameter
        {
            Name = "environment",
            DisplayName = "Environment",
            Description = "The environment to deploy to",
            Type = ParameterType.Select,
            IsRequired = true,
            PossibleValues = new[] { "Development", "Testing", "Staging", "Production" }
        },
        
        // Version to deploy
        new PluginParameter
        {
            Name = "version",
            DisplayName = "Version",
            Description = "The version to deploy (format: x.y.z)",
            Type = ParameterType.String,
            IsRequired = true
        },
        
        // Git repositories
        new PluginParameter
        {
            Name = "repositories",
            DisplayName = "Git Repositories",
            Description = "Comma-separated list of Git repositories to include in the deployment",
            Type = ParameterType.String,
            IsRequired = true,
            DefaultValue = "main-repo"
        },
        
        // Database migration option
        new PluginParameter
        {
            Name = "runDatabaseMigrations",
            DisplayName = "Run Database Migrations",
            Description = "Whether to run database migrations as part of the deployment",
            Type = ParameterType.Boolean,
            DefaultValue = "true"
        },
        
        // Environment variables
        new PluginParameter
        {
            Name = "environmentVariables",
            DisplayName = "Environment Variables",
            Description = "JSON string of environment variables to set (e.g., {\"VAR1\":\"value1\",\"VAR2\":\"value2\"})",
            Type = ParameterType.String,
            DefaultValue = "{}"
        },
        
        // Notification emails
        new PluginParameter
        {
            Name = "notificationEmails",
            DisplayName = "Notification Emails",
            Description = "Comma-separated list of email addresses to notify on completion",
            Type = ParameterType.String
        },
        
        // Skip tests option
        new PluginParameter
        {
            Name = "skipTests",
            DisplayName = "Skip Tests",
            Description = "Whether to skip running tests during deployment",
            Type = ParameterType.Boolean,
            DefaultValue = "false"
        },
        
        // Deployment timeout
        new PluginParameter
        {
            Name = "timeoutMinutes",
            DisplayName = "Timeout (Minutes)",
            Description = "Maximum time in minutes to wait for deployment to complete",
            Type = ParameterType.Number,
            DefaultValue = "30"
        },
        
        // Rollback on failure option
        new PluginParameter
        {
            Name = "rollbackOnFailure",
            DisplayName = "Rollback On Failure",
            Description = "Whether to automatically rollback if deployment fails",
            Type = ParameterType.Boolean,
            DefaultValue = "true"
        }
    };

    /// <inheritdoc/>
    public async Task<PluginResult> TriggerAsync(IDictionary<string, string> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Triggering SingleTenantDeployment with parameters: {@Parameters}", parameters);
            
            // Required parameters validation
            if (!parameters.TryGetValue("product", out var product) || string.IsNullOrEmpty(product))
            {
                return PluginResult.Failure("Product is required.");
            }
            
            if (!parameters.TryGetValue("environment", out var environment) || string.IsNullOrEmpty(environment))
            {
                return PluginResult.Failure("Environment is required.");
            }
            
            if (!parameters.TryGetValue("version", out var version) || string.IsNullOrEmpty(version))
            {
                return PluginResult.Failure("Version is required.");
            }
            
            if (!parameters.TryGetValue("repositories", out var repositories) || string.IsNullOrEmpty(repositories))
            {
                return PluginResult.Failure("Repositories are required.");
            }
            
            // Optional parameters with defaults
            var runDatabaseMigrations = parameters.TryGetValue("runDatabaseMigrations", out var dbMigrations) && 
                                       !string.IsNullOrEmpty(dbMigrations) && 
                                       bool.TryParse(dbMigrations, out var dbResult) && 
                                       dbResult;
            
            var environmentVariables = parameters.TryGetValue("environmentVariables", out var envVars) ? 
                                      envVars : "{}";
            
            var notificationEmails = parameters.TryGetValue("notificationEmails", out var emails) ? 
                                    emails : string.Empty;
            
            var skipTests = parameters.TryGetValue("skipTests", out var tests) && 
                           !string.IsNullOrEmpty(tests) && 
                           bool.TryParse(tests, out var testsResult) && 
                           testsResult;
            
            var timeoutMinutes = parameters.TryGetValue("timeoutMinutes", out var timeout) && 
                               int.TryParse(timeout, out var timeoutResult) ? 
                               timeoutResult : 30;
            
            var rollbackOnFailure = !parameters.TryGetValue("rollbackOnFailure", out var rollback) || 
                                   string.IsNullOrEmpty(rollback) || 
                                   !bool.TryParse(rollback, out var rollbackResult) || 
                                   rollbackResult;

            // Prepare Jenkins job parameters
            var jenkinsParams = new Dictionary<string, string>
            {
                ["PRODUCT"] = product,
                ["ENVIRONMENT"] = environment,
                ["VERSION"] = version,
                ["REPOSITORIES"] = repositories,
                ["RUN_DB_MIGRATIONS"] = runDatabaseMigrations.ToString().ToLowerInvariant(),
                ["ENV_VARS"] = environmentVariables,
                ["NOTIFICATION_EMAILS"] = notificationEmails,
                ["SKIP_TESTS"] = skipTests.ToString().ToLowerInvariant(),
                ["TIMEOUT_MINUTES"] = timeoutMinutes.ToString(),
                ["ROLLBACK_ON_FAILURE"] = rollbackOnFailure.ToString().ToLowerInvariant()
            };
            
            // Trigger the Jenkins build
            _logger.LogInformation("Triggering Jenkins job for SingleTenantDeployment with parameters: {@JenkinsParameters}", jenkinsParams);
            var jenkinsResult = await _jenkinsService.TriggerBuildAsync("deploy-single-tenant", jenkinsParams, cancellationToken);
            
            if (!jenkinsResult.IsSuccessful)
            {
                _logger.LogError("Failed to trigger Jenkins job: {ErrorMessage}", jenkinsResult.ErrorMessage);
                return PluginResult.Failure(
                    jenkinsResult.ErrorMessage ?? "Failed to trigger Jenkins job.", 
                    "The Jenkins server returned an error when attempting to trigger the build.");
            }
            
            // Prepare the logs
            var logs = new List<string>
            {
                $"Started deployment of {product} version {version} to {environment}",
                $"Jenkins build #{jenkinsResult.BuildNumber} triggered successfully",
                $"Build URL: {jenkinsResult.BuildUrl}",
                $"Estimated build duration: {TimeSpan.FromMilliseconds(jenkinsResult.EstimatedDuration):g}",
                $"Build started at: {jenkinsResult.Timestamp:yyyy-MM-dd HH:mm:ss}"
            };
            
            // Return successful result with details
            return PluginResult.Success(
                data: new
                {
                    jenkinsResult.BuildNumber,
                    jenkinsResult.BuildUrl,
                    Product = product,
                    Environment = environment,
                    Version = version,
                    Repositories = repositories.Split(',').Select(r => r.Trim()).ToArray(),
                    RunDatabaseMigrations = runDatabaseMigrations,
                    SkipTests = skipTests,
                    RollbackOnFailure = rollbackOnFailure,
                    TimeoutMinutes = timeoutMinutes,
                    EstimatedCompletion = DateTime.UtcNow.AddMilliseconds(jenkinsResult.EstimatedDuration)
                },
                details: $"Successfully triggered deployment of {product} version {version} to {environment}. Jenkins build #{jenkinsResult.BuildNumber} has been started.",
                logs: logs
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering SingleTenantDeployment: {ErrorMessage}", ex.Message);
            return PluginResult.Failure(
                "An error occurred while triggering the deployment.", 
                ex.ToString());
        }
    }
}
