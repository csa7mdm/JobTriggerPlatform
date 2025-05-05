using System.Text;

namespace JobTriggerPlatform.WebApi.Middleware;

/// <summary>
/// Middleware to limit request input size.
/// </summary>
public class InputSizeLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputSizeLimitMiddleware> _logger;
    private readonly int _maxRequestBodySize;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputSizeLimitMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="maxRequestBodySizeInBytes">The maximum request body size in bytes.</param>
    public InputSizeLimitMiddleware(RequestDelegate next, ILogger<InputSizeLimitMiddleware> logger, int maxRequestBodySizeInBytes = 1024 * 1024) // 1MB default
    {
        _next = next;
        _logger = logger;
        _maxRequestBodySize = maxRequestBodySizeInBytes;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Only check POST, PUT, and PATCH requests
        if (context.Request.Method == HttpMethods.Post || 
            context.Request.Method == HttpMethods.Put || 
            context.Request.Method == HttpMethods.Patch)
        {
            context.Request.EnableBuffering();

            // Check content length header
            if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > _maxRequestBodySize)
            {
                _logger.LogWarning("Request with content length {ContentLength} bytes exceeds the limit of {MaxSize} bytes", 
                    context.Request.ContentLength.Value, _maxRequestBodySize);
                
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                context.Response.ContentType = "application/json";
                
                var json = "{ \"error\": \"Request payload too large\", \"maxSize\": \"" + _maxRequestBodySize + " bytes\" }";
                await context.Response.WriteAsync(json, Encoding.UTF8);
                
                return;
            }

            // For requests without content length, we'll check the actual body size
            using var memoryStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(memoryStream);
            
            if (memoryStream.Length > _maxRequestBodySize)
            {
                _logger.LogWarning("Request with body size {BodySize} bytes exceeds the limit of {MaxSize} bytes", 
                    memoryStream.Length, _maxRequestBodySize);
                
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                context.Response.ContentType = "application/json";
                
                var json = "{ \"error\": \"Request payload too large\", \"maxSize\": \"" + _maxRequestBodySize + " bytes\" }";
                await context.Response.WriteAsync(json, Encoding.UTF8);
                
                return;
            }

            // Reset the position of the stream
            context.Request.Body.Position = 0;
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for the InputSizeLimitMiddleware.
/// </summary>
public static class InputSizeLimitMiddlewareExtensions
{
    /// <summary>
    /// Adds the input size limit middleware to the pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="maxRequestBodySizeInBytes">The maximum request body size in bytes.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseInputSizeLimit(this IApplicationBuilder builder, int maxRequestBodySizeInBytes = 1024 * 1024)
    {
        return builder.UseMiddleware<InputSizeLimitMiddleware>(maxRequestBodySizeInBytes);
    }
}
