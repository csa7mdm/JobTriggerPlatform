namespace JobTriggerPlatform.WebApi.Models;

/// <summary>
/// Represents a request to trigger a job.
/// </summary>
public class JobTriggerRequest
{
    /// <summary>
    /// Gets or sets the parameters for the job.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
}
