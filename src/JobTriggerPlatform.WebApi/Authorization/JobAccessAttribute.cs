using Microsoft.AspNetCore.Authorization;

namespace JobTriggerPlatform.WebApi.Authorization
{
    /// <summary>
    /// Authorization attribute for job access.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class JobAccessAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobAccessAttribute"/> class.
        /// </summary>
        /// <param name="jobName">The name of the job to authorize access for.</param>
        public JobAccessAttribute(string jobName) : base("JobAccess")
        {
            JobName = jobName;
        }

        /// <summary>
        /// Gets the name of the job.
        /// </summary>
        public string JobName { get; }
    }
}