using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace JobTriggerPlatform.WebApi.RateLimiting;

/// <summary>
/// Rate limiter policy that limits requests based on user ID.
/// </summary>
public class UserBasedRateLimiterPolicy : IRateLimiterPolicy<string>
{
    private readonly int _permitLimit;
    private readonly TimeSpan _queueProcessingOrder;
    private readonly int _queueLimit;
    private readonly ILogger<UserBasedRateLimiterPolicy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserBasedRateLimiterPolicy"/> class.
    /// </summary>
    /// <param name="permitLimit">The number of permits per time window.</param>
    /// <param name="windowInMinutes">The time window in minutes.</param>
    /// <param name="queueLimit">The queue limit.</param>
    /// <param name="logger">The logger.</param>
    public UserBasedRateLimiterPolicy(
        int permitLimit,
        int windowInMinutes,
        int queueLimit,
        ILogger<UserBasedRateLimiterPolicy> logger)
    {
        _permitLimit = permitLimit;
        _queueProcessingOrder = TimeSpan.FromMinutes(windowInMinutes);
        _queueLimit = queueLimit;
        _logger = logger;
    }

    /// <inheritdoc/>
    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetTokenBucketLimiter(userId, key =>
            new TokenBucketRateLimiterOptions
            {
                TokenLimit = _permitLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = _queueLimit,
                ReplenishmentPeriod = _queueProcessingOrder,
                TokensPerPeriod = _permitLimit,
                AutoReplenishment = true
            });
    }

    /// <inheritdoc/>
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected =>
        async (context, token) =>
        {
            var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                         context.HttpContext.Connection.RemoteIpAddress?.ToString() ??
                         "anonymous";

            _logger.LogWarning("Rate limit exceeded for user {UserId}. Retry after {RetryAfter}",
                userId, context.Lease.GetRetryAfterMetadata());

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            // Get retry after metadata if available
            TimeSpan? retryAfter = null;
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out object? metadataValue) && 
                metadataValue is TimeSpan timeSpanValue)
            {
                retryAfter = timeSpanValue;
            }

            if (retryAfter.HasValue)
            {
                context.HttpContext.Response.Headers.RetryAfter = retryAfter.Value.TotalSeconds.ToString();
            }
            context.HttpContext.Response.ContentType = "application/json";

            var json = "{ \"error\": \"Too many requests\"";
            if (retryAfter.HasValue)
            {
                json += $", \"retryAfter\": \"{retryAfter.Value.TotalSeconds}\"";
            }
            json += " }";

            await context.HttpContext.Response.WriteAsync(json, token);
        };
}
