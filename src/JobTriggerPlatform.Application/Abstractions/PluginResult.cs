namespace JobTriggerPlatform.Application.Abstractions;

/// <summary>
/// Represents the result of a plugin operation.
/// </summary>
public class PluginResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the data returned by the plugin, if any.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets additional details about the operation result.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets the logs generated during the operation.
    /// </summary>
    public IReadOnlyCollection<string>? Logs { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="data">Optional data to include in the result.</param>
    /// <param name="details">Optional details about the operation.</param>
    /// <param name="logs">Optional logs generated during the operation.</param>
    /// <returns>A successful plugin result.</returns>
    public static PluginResult Success(object? data = null, string? details = null, IReadOnlyCollection<string>? logs = null)
    {
        return new PluginResult
        {
            IsSuccess = true,
            Data = data,
            Details = details,
            Logs = logs
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="details">Optional details about the error.</param>
    /// <param name="logs">Optional logs generated during the operation.</param>
    /// <returns>A failed plugin result.</returns>
    public static PluginResult Failure(string errorMessage, string? details = null, IReadOnlyCollection<string>? logs = null)
    {
        return new PluginResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Details = details,
            Logs = logs
        };
    }
}
