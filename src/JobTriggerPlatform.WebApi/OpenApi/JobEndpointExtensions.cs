using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;

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
        endpoints.WithOpenApi(operation => new OpenApiOperation(operation)
        {
            Tags = new List<OpenApiTag> { new() { Name = "Jobs" } },
            Summary = "Job management endpoints",
            Description = "Endpoints for managing and triggering jobs"
        });

        return endpoints;
    }

    /// <summary>
    /// Adds OpenAPI documentation for the get jobs endpoint.
    /// </summary>
    /// <param name="endpoint">The route handler builder.</param>
    /// <returns>The route handler builder.</returns>
    public static RouteHandlerBuilder WithGetJobsOpenApi(this RouteHandlerBuilder endpoint)
    {
        return endpoint.WithOpenApi(operation =>
        {
            operation.Summary = "Get all jobs";
            operation.Description = "Gets all jobs that the current user has access to.";
            operation.Tags = new List<OpenApiTag> { new() { Name = "Jobs" } };
            operation.Responses[StatusCodes.Status200OK.ToString()].Description = "A list of jobs that the user has access to.";
            operation.Responses[StatusCodes.Status401Unauthorized.ToString()] = new OpenApiResponse
            {
                Description = "User is not authenticated."
            };
            
            return operation;
        });
    }

    /// <summary>
    /// Adds OpenAPI documentation for the get job endpoint.
    /// </summary>
    /// <param name="endpoint">The route handler builder.</param>
    /// <returns>The route handler builder.</returns>
    public static RouteHandlerBuilder WithGetJobOpenApi(this RouteHandlerBuilder endpoint)
    {
        return endpoint.WithOpenApi(operation =>
        {
            operation.Summary = "Get job details";
            operation.Description = "Gets details for a specific job.";
            operation.Tags = new List<OpenApiTag> { new() { Name = "Jobs" } };
            operation.Parameters[0].Description = "The name of the job to get details for.";
            operation.Responses[StatusCodes.Status200OK.ToString()].Description = "The job details.";
            operation.Responses[StatusCodes.Status401Unauthorized.ToString()] = new OpenApiResponse
            {
                Description = "User is not authenticated."
            };
            operation.Responses[StatusCodes.Status403Forbidden.ToString()] = new OpenApiResponse
            {
                Description = "User does not have access to the job."
            };
            operation.Responses[StatusCodes.Status404NotFound.ToString()] = new OpenApiResponse
            {
                Description = "Job not found."
            };
            
            return operation;
        });
    }

    /// <summary>
    /// Adds OpenAPI documentation for the trigger job endpoint.
    /// </summary>
    /// <param name="endpoint">The route handler builder.</param>
    /// <returns>The route handler builder.</returns>
    public static RouteHandlerBuilder WithTriggerJobOpenApi(this RouteHandlerBuilder endpoint)
    {
        return endpoint.WithOpenApi(operation =>
        {
            operation.Summary = "Trigger a job";
            operation.Description = "Triggers a job with the provided parameters.";
            operation.Tags = new List<OpenApiTag> { new() { Name = "Jobs" } };
            operation.Parameters[0].Description = "The name of the job to trigger.";
            operation.RequestBody.Description = "The parameters for the job.";
            operation.Responses[StatusCodes.Status200OK.ToString()].Description = "The job was triggered successfully.";
            operation.Responses[StatusCodes.Status400BadRequest.ToString()] = new OpenApiResponse
            {
                Description = "Invalid parameters or the job execution failed."
            };
            operation.Responses[StatusCodes.Status401Unauthorized.ToString()] = new OpenApiResponse
            {
                Description = "User is not authenticated."
            };
            operation.Responses[StatusCodes.Status403Forbidden.ToString()] = new OpenApiResponse
            {
                Description = "User does not have access to the job."
            };
            operation.Responses[StatusCodes.Status404NotFound.ToString()] = new OpenApiResponse
            {
                Description = "Job not found."
            };
            operation.Responses[StatusCodes.Status500InternalServerError.ToString()] = new OpenApiResponse
            {
                Description = "An error occurred while triggering the job."
            };
            
            return operation;
        });
    }
}
