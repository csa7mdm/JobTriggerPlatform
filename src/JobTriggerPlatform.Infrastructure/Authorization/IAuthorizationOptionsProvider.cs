namespace JobTriggerPlatform.Infrastructure.Authorization;

/// <summary>
/// Interface for providing authorization options.
/// </summary>
public interface IAuthorizationOptionsProvider
{
    /// <summary>
    /// Adds a job access policy for the specified job.
    /// </summary>
    /// <param name="jobName">The name of the job.</param>
    void AddJobAccessPolicy(string jobName);
}
