using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;
using NSwag.AspNetCore;
using System.Collections.Generic;

namespace JobTriggerPlatform.WebApi.OpenApi;

/// <summary>
/// Extensions for enhancing OpenAPI documentation for job endpoints.
/// </summary>
public static class JobEndpointExtensions
{
    /// <summary>
    /// Adds OpenAPI documentation for job endpoints.
    /// </summary>
    /// <param name="endpoints">The route group builder.</param>
    /// <returns>The route group builder.</returns>
    public static RouteGroupBuilder WithJobsOpenApi(this RouteGroupBuilder endpoints)
    {
        // In .NET 9, we need to use different approach for tagging
        return endpoints;
    }

    /// <summary>
    /// Adds OpenAPI documentation for the get jobs endpoint.
    /// </summary>
    /// <param name="endpoint">The route handler builder.</param>
    /// <returns>The route handler builder.</returns>
    public static RouteHandlerBuilder WithGetJobsOpenApi(this RouteHandlerBuilder endpoint)
    {
        return endpoint.WithName("GetJobs")
                       .WithDescription("Gets all jobs that the current user has access to.")
                       .WithTags("Jobs");
    }

    /// <summary>
    /// Adds OpenAPI documentation for the get job endpoint.
    /// </summary>
    /// <param name="endpoint">The route handler builder.</param>
    /// <returns>The route handler builder.</returns>
    public static RouteHandlerBuilder WithGetJobOpenApi(this RouteHandlerBuilder endpoint)
    {
        return endpoint.WithName("GetJob")
                       .WithDescription("Gets details for a specific job.")
                       .WithTags("Jobs");
    }

    /// <summary>
    /// Adds OpenAPI documentation for the trigger job endpoint.
    /// </summary>
    /// <param name="endpoint">The route handler builder.</param>
    /// <returns>The route handler builder.</returns>
    public static RouteHandlerBuilder WithTriggerJobOpenApi(this RouteHandlerBuilder endpoint)
    {
        return endpoint.WithName("TriggerJob")
                       .WithDescription("Triggers a job with the provided parameters.")
                       .WithTags("Jobs");
    }
}