using Serilog.Context;

namespace JobTriggerPlatform.WebApi.Middleware;

/// <summary>
/// Middleware that adds the user name to the Serilog LogContext.
/// </summary>
public class LogUserNameMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogUserNameMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public LogUserNameMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            using (LogContext.PushProperty("User", context.User.Identity.Name))
            {
                await _next(context);
            }
        }
        else
        {
            await _next(context);
        }
    }
}

/// <summary>
/// Extension methods for the LogUserNameMiddleware.
/// </summary>
public static class LogUserNameMiddlewareExtensions
{
    /// <summary>
    /// Adds the log user name middleware to the pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseLogUserName(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LogUserNameMiddleware>();
    }
}
