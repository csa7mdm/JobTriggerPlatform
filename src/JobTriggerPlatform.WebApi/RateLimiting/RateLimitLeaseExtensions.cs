using System.Threading.RateLimiting;

namespace JobTriggerPlatform.WebApi.RateLimiting;

/// <summary>
/// Extension methods for working with RateLimitLease
/// </summary>
public static class RateLimitLeaseExtensions
{
    /// <summary>
    /// Gets the retry after value from a rate limit lease if available.
    /// </summary>
    /// <param name="lease">The rate limit lease.</param>
    /// <returns>A string representation of the retry after value, or "unknown" if not available.</returns>
    public static string GetRetryAfterMetadata(this RateLimitLease lease)
    {
        TimeSpan? retryAfter = null;
        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterTimeSpan))
        {
            retryAfter = retryAfterTimeSpan;
        }

        return retryAfter?.ToString() ?? "unknown";
    }
}
