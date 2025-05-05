namespace JobTriggerPlatform.Application.Abstractions;

/// <summary>
/// Defines the contract for a job trigger plugin.
/// Plugin implementations should provide deployment automation functionality.
/// </summary>
public interface IJobTriggerPlugin
{
    /// <summary>
    /// Gets the name of the job that this plugin handles.
    /// </summary>
    string JobName { get; }

    /// <summary>
    /// Gets the roles that are required to trigger this job.
    /// </summary>
    IReadOnlyCollection<string> RequiredRoles { get; }

    /// <summary>
    /// Gets the parameters that this job requires to be triggered.
    /// </summary>
    IReadOnlyCollection<PluginParameter> Parameters { get; }

    /// <summary>
    /// Triggers the job with the provided parameters.
    /// </summary>
    /// <param name="parameters">The parameters for the job.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<PluginResult> TriggerAsync(IDictionary<string, string> parameters, CancellationToken cancellationToken = default);
}
