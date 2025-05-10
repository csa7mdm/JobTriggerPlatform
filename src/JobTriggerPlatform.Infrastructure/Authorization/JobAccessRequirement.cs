using Microsoft.AspNetCore.Authorization;

namespace JobTriggerPlatform.Infrastructure.Authorization
{
    /// <summary>
    /// Authorization requirement for job access.
    /// </summary>
    public class JobAccessRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobAccessRequirement"/> class.
        /// </summary>
        /// <param name="jobName">The name of the job to authorize access for.</param>
        public JobAccessRequirement(string jobName)
        {
            JobName = jobName;
        }

        /// <summary>
        /// Gets the name of the job.
        /// </summary>
        public string JobName { get; }
    }
}