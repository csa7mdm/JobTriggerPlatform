namespace JobTriggerPlatform.Application.Abstractions
{
    /// <summary>
    /// Represents the result of a job trigger operation.
    /// </summary>
    public class JobTriggerResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the job trigger was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the message associated with the job trigger result.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets additional details about the job trigger result.
        /// </summary>
        public Dictionary<string, string>? Details { get; set; }

        /// <summary>
        /// Gets or sets the specific error message in case of failure.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the collection of log messages from the job execution.
        /// </summary>
        public IEnumerable<string>? Logs { get; set; }
    }
}