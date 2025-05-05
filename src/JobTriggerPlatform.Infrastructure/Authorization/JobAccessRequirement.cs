using Microsoft.AspNetCore.Authorization;

namespace JobTriggerPlatform.Infrastructure.Authorization;

/// <summary>
/// Authorization requirement that checks if a user has access to a job.
/// </summary>
public class JobAccessRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the name of the job to check access for.
    /// </summary>
    public string JobName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobAccessRequirement"/> class.
    /// </summary>
    /// <param name="jobName">The name of the job to check access for.</param>
    public JobAccessRequirement(string jobName)
    {
        JobName = jobName;
    }
}
