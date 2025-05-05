using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using System.Security.Claims;

namespace JobTriggerPlatform.WebApi.Logging;

/// <summary>
/// Enriches log events with the ID of the user making the request.
/// </summary>
public class RequestUserIdEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string PropertyName = "UserId";

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestUserIdEnricher"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public RequestUserIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Enriches the log event with the user ID from the HTTP context.
    /// </summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">The property factory.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (_httpContextAccessor.HttpContext == null)
        {
            return;
        }

        var userId = _httpContextAccessor.HttpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var userIdProperty = propertyFactory.CreateProperty(PropertyName, userId);
        logEvent.AddPropertyIfAbsent(userIdProperty);
    }
}
