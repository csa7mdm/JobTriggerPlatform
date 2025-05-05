namespace JobTriggerPlatform.Application.Abstractions;

/// <summary>
/// Service interface for interacting with Jenkins CI/CD.
/// </summary>
public interface IJenkinsService
{
    /// <summary>
    /// Triggers a Jenkins build with the specified parameters.
    /// </summary>
    /// <param name="jobName">The name of the Jenkins job to trigger.</param>
    /// <param name="parameters">The build parameters.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation with build information.</returns>
    Task<JenkinsBuildResult> TriggerBuildAsync(string jobName, Dictionary<string, string> parameters, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of triggering a Jenkins build.
/// </summary>
public class JenkinsBuildResult
{
    /// <summary>
    /// Gets or sets the build number.
    /// </summary>
    public int BuildNumber { get; set; }

    /// <summary>
    /// Gets or sets the URL to view the build.
    /// </summary>
    public string? BuildUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the build was successfully triggered.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Gets or sets the error message if the build failed to trigger.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the estimated duration of the build in milliseconds.
    /// </summary>
    public long EstimatedDuration { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the build was triggered.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
