using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.Extensions;

namespace JobTriggerPlatform.WebApi.Middleware;

/// <summary>
/// Middleware that sets and validates an anti-forgery token for API calls.
/// </summary>
public class ApiAntiForgeryCookieMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<ApiAntiForgeryCookieMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiAntiForgeryCookieMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="antiforgery">The antiforgery service.</param>
    /// <param name="logger">The logger.</param>
    public ApiAntiForgeryCookieMiddleware(
        RequestDelegate next,
        IAntiforgery antiforgery,
        ILogger<ApiAntiForgeryCookieMiddleware> logger)
    {
        _next = next;
        _antiforgery = antiforgery;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Exclude OPTIONS requests, Swagger requests, and Auth endpoints from anti-forgery check
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        _logger.LogInformation($"[AntiForgery] Method: {context.Request.Method}, Path: {path}");
        if (context.Request.Method == HttpMethods.Options ||
            path.StartsWith("/swagger") ||
            path.StartsWith("/api/auth/"))
        {
            await _next(context);
            return;
        }

        // For GET, HEAD, and OPTIONS requests, we don't need to validate the anti-forgery token,
        // but we'll generate one if the user is authenticated
        if (context.Request.Method == HttpMethods.Get || 
            context.Request.Method == HttpMethods.Head || 
            context.Request.Method == HttpMethods.Options)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // Generate the tokens
                var tokens = _antiforgery.GetAndStoreTokens(context);

                // Set the X-XSRF-TOKEN header (for AJAX requests)
                context.Response.Headers.Add("X-XSRF-TOKEN", tokens.RequestToken ?? "");
            }

            await _next(context);
            return;
        }

        // For all other methods (POST, PUT, DELETE, etc.), validate the anti-forgery token
        try
        {
            await _antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException ex)
        {
            _logger.LogWarning(
                ex,
                "Anti-forgery token validation failed for {Method} request to {Url}",
                context.Request.Method,
                context.Request.GetDisplayUrl());

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            
            await context.Response.WriteAsync("{\"error\": \"Invalid anti-forgery token\", \"details\": \"Please include a valid anti-forgery token in your request\"}");
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for the ApiAntiForgeryCookieMiddleware.
/// </summary>
public static class ApiAntiForgeryCookieMiddlewareExtensions
{
    /// <summary>
    /// Adds the API anti-forgery cookie middleware to the pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseApiAntiForgery(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiAntiForgeryCookieMiddleware>();
    }
}
