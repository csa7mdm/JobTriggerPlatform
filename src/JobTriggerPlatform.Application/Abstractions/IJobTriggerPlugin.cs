namespace JobTriggerPlatform.Application.Abstractions
{
    /// <summary>
    /// Interface for job trigger plugins.
    /// </summary>
    public interface IJobTriggerPlugin
    {
        /// <summary>
        /// Gets the name of the job.
        /// </summary>
        string JobName { get; }

        /// <summary>
        /// Gets the parameters for the job.
        /// </summary>
        IEnumerable<PluginParameter> Parameters { get; }

        /// <summary>
        /// Gets the roles required to access this job.
        /// </summary>
        IEnumerable<string>? RequiredRoles { get; }

        /// <summary>
        /// Triggers the job with the specified parameters.
        /// </summary>
        /// <param name="parameters">The parameters for the job.</param>
        /// <returns>The result of the job trigger operation.</returns>
        Task<PluginResult> TriggerAsync(Dictionary<string, string> parameters);
    }
}