using JobTriggerPlatform.Application.Abstractions;
using JobTriggerPlatform.Infrastructure.Authorization;
using JobTriggerPlatform.WebApi.Models;
using JobTriggerPlatform.WebApi.OpenApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobTriggerPlatform.WebApi;

/// <summary>
/// Extension methods for configuring job endpoints.
/// </summary>
public static class JobEndpoints
{
    /// <summary>
    /// Maps the job endpoints to the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>A collection of endpoint route handlers.</returns>
    public static IEnumerable<RouteHandlerBuilder> MapJobEndpoints(this WebApplication app)
    {
        var jobsGroup = app.MapGroup("/api/jobs")
            .WithTags("Jobs");

        var endpoints = new List<RouteHandlerBuilder>();

        // Get all available jobs
        var getJobsEndpoint = jobsGroup.MapGet("/", GetJobs)
            .WithName("GetJobs")
            .RequireAuthorization()
            .WithDescription("Gets all jobs that the current user has access to.")
            .WithTags("Jobs");
        endpoints.Add(getJobsEndpoint);

        // Get job details
        var getJobEndpoint = jobsGroup.MapGet("/{name}", GetJob)
            .WithName("GetJob")
            .RequireAuthorization(policy => policy.RequireJobAccess())
            .WithDescription("Gets details for a specific job.")
            .WithTags("Jobs");
        endpoints.Add(getJobEndpoint);

        // Trigger a job
        var triggerJobEndpoint = jobsGroup.MapPost("/{name}", TriggerJob)
            .WithName("TriggerJob")
            .RequireAuthorization(policy => policy.RequireJobAccess())
            .Accepts<JobTriggerRequest>("application/json")
            .WithDescription("Triggers a job with the provided parameters.")
            .WithTags("Jobs");
        endpoints.Add(triggerJobEndpoint);

        // Get all plugins
        var getPluginsEndpoint = jobsGroup.MapGet("/plugins", GetPlugins)
            .WithName("GetPlugins")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithDescription("Gets all available plugins (Admin only).")
            .WithTags("Plugins");
        endpoints.Add(getPluginsEndpoint);

        // Get plugin details
        var getPluginEndpoint = jobsGroup.MapGet("/plugins/{name}", GetPlugin)
            .WithName("GetPlugin")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithDescription("Gets details for a specific plugin (Admin only).")
            .WithTags("Plugins");
        endpoints.Add(getPluginEndpoint);

        // Get job history
        var getJobHistoryEndpoint = jobsGroup.MapGet("/history", GetJobHistory)
            .WithName("GetJobHistory")
            .RequireAuthorization()
            .WithDescription("Gets history for all jobs the user has access to.")
            .WithTags("History");
        endpoints.Add(getJobHistoryEndpoint);

        // Get job history for a specific job
        var getJobHistoryByNameEndpoint = jobsGroup.MapGet("/history/{name}", GetJobHistoryByName)
            .WithName("GetJobHistoryByName")
            .RequireAuthorization(policy => policy.RequireJobAccess())
            .WithDescription("Gets history for a specific job.")
            .WithTags("History");
        endpoints.Add(getJobHistoryByNameEndpoint);

        return endpoints;
    }

    /// <summary>
    /// Gets all available jobs.
    /// </summary>
    /// <param name="plugins">The job trigger plugins.</param>
    /// <param name="user">The user.</param>
    /// <returns>The list of available jobs.</returns>
    private static IResult GetJobs(
        [FromServices] IEnumerable<IJobTriggerPlugin> plugins,
        ClaimsPrincipal user)
    {
        var userRoles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var jobAccessClaims = user.FindAll("JobAccess").Select(c => c.Value).ToList();

        var accessibleJobs = plugins
            .Where(p =>
                // User has direct job access via claim
                jobAccessClaims.Contains(p.JobName) ||
                // User has a role that is required for the job
                p.RequiredRoles.Any(role => userRoles.Contains(role)))
            .Select(p => new
            {
                p.JobName,
                Parameters = p.Parameters.Select(param => new
                {
                    param.Name,
                    param.DisplayName,
                    param.Description,
                    param.IsRequired,
                    Type = param.Type.ToString(),
                    param.DefaultValue,
                    PossibleValues = param.PossibleValues
                }).ToList()
            });

        return Results.Ok(accessibleJobs);
    }

    /// <summary>
    /// Gets details for a specific job.
    /// </summary>
    /// <param name="name">The job name.</param>
    /// <param name="plugins">The job trigger plugins.</param>
    /// <returns>The job details.</returns>
    private static IResult GetJob(
        string name,
        [FromServices] IEnumerable<IJobTriggerPlugin> plugins)
    {
        var plugin = plugins.FirstOrDefault(p => p.JobName == name);
        if (plugin == null)
        {
            return Results.NotFound($"Job '{name}' not found.");
        }

        return Results.Ok(new
        {
            plugin.JobName,
            Parameters = plugin.Parameters.Select(param => new
            {
                param.Name,
                param.DisplayName,
                param.Description,
                param.IsRequired,
                Type = param.Type.ToString(),
                param.DefaultValue,
                PossibleValues = param.PossibleValues
            }).ToList()
        });
    }

    /// <summary>
    /// Triggers a job.
    /// </summary>
    /// <param name="name">The job name.</param>
    /// <param name="request">The job trigger request.</param>
    /// <param name="plugins">The job trigger plugins.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The result of the job trigger.</returns>
    private static async Task<IResult> TriggerJob(
        string name,
        [FromBody] JobTriggerRequest request,
        [FromServices] IEnumerable<IJobTriggerPlugin> plugins,
        [FromServices] ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        var plugin = plugins.FirstOrDefault(p => p.JobName == name);
        if (plugin == null)
        {
            return Results.NotFound($"Job '{name}' not found.");
        }

        try
        {
            // Create and run the validator for the request
            var validator = new JobTriggerRequestValidator();
            var validationResult = await validator.ValidateAsync(request);

            // Perform manual validation for the plugin-specific parameters
            var validationErrors = new Dictionary<string, string[]>();

            // Validate that all required parameters are present
            foreach (var parameter in plugin.Parameters.Where(p => p.IsRequired))
            {
                if (!request.Parameters.ContainsKey(parameter.Name) || 
                    string.IsNullOrEmpty(request.Parameters[parameter.Name]))
                {
                    validationErrors[$"Parameters.{parameter.Name}"] = 
                        new[] { $"Parameter '{parameter.Name}' is required." };
                }
            }

            // Validate parameters with specific types
            foreach (var parameter in plugin.Parameters)
            {
                if (!request.Parameters.ContainsKey(parameter.Name) || 
                    string.IsNullOrEmpty(request.Parameters[parameter.Name]))
                {
                    continue;
                }

                switch (parameter.Type)
                {
                    case ParameterType.Number:
                        if (!decimal.TryParse(request.Parameters[parameter.Name], out _))
                        {
                            validationErrors[$"Parameters.{parameter.Name}"] = 
                                new[] { $"Parameter '{parameter.Name}' must be a valid number." };
                        }
                        break;

                    case ParameterType.Boolean:
                        if (!bool.TryParse(request.Parameters[parameter.Name], out _))
                        {
                            validationErrors[$"Parameters.{parameter.Name}"] = 
                                new[] { $"Parameter '{parameter.Name}' must be a valid boolean (true/false)." };
                        }
                        break;

                    case ParameterType.Date:
                        if (!DateTime.TryParse(request.Parameters[parameter.Name], out _))
                        {
                            validationErrors[$"Parameters.{parameter.Name}"] = 
                                new[] { $"Parameter '{parameter.Name}' must be a valid date." };
                        }
                        break;

                    case ParameterType.Select:
                        if (parameter.PossibleValues != null && 
                            !parameter.PossibleValues.Contains(request.Parameters[parameter.Name]))
                        {
                            validationErrors[$"Parameters.{parameter.Name}"] = 
                                new[] { $"Parameter '{parameter.Name}' must be one of: {string.Join(", ", parameter.PossibleValues)}." };
                        }
                        break;
                }
            }

            // Combine FluentValidation results with manual validation
            if (!validationResult.IsValid || validationErrors.Count > 0)
            {
                var combinedErrors = validationResult.ToDictionary();
                foreach (var error in validationErrors)
                {
                    combinedErrors[error.Key] = error.Value;
                }

                return Results.ValidationProblem(combinedErrors);
            }

            logger.LogInformation("Triggering job {JobName} with parameters: {@Parameters}", name, request.Parameters);

            var result = await plugin.TriggerAsync(request.Parameters);

            if (result.IsSuccess)
            {
                // Add to job history (in a real application, this would be stored in a database)
                // This is a placeholder for demonstration purposes
                logger.LogInformation("Job {JobName} completed successfully: {@Result}", name, result);

                return Results.Ok(new
                {
                    Success = true,
                    JobName = name,
                    ExecutedAt = DateTime.UtcNow,
                    Parameters = request.Parameters,
                    Result = result
                });
            }
            else
            {
                logger.LogWarning("Job {JobName} failed: {ErrorMessage}", name, result.ErrorMessage);

                return Results.BadRequest(new
                {
                    Success = false,
                    JobName = name,
                    ExecutedAt = DateTime.UtcNow,
                    Parameters = request.Parameters,
                    ErrorMessage = result.ErrorMessage,
                    Details = result.Details,
                    Logs = result.Logs
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error triggering job {JobName}", name);
            return Results.Problem(
                title: "Job Execution Failed",
                detail: "An error occurred while triggering the job.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Gets all plugins (admin only).
    /// </summary>
    /// <param name="plugins">The job trigger plugins.</param>
    /// <returns>The list of plugins.</returns>
    private static IResult GetPlugins([FromServices] IEnumerable<IJobTriggerPlugin> plugins)
    {
        var pluginList = plugins.Select(p => new
        {
            p.JobName,
            RequiredRoles = p.RequiredRoles.ToList(),
            Parameters = p.Parameters.Select(param => new
            {
                param.Name,
                param.DisplayName,
                param.Description,
                param.IsRequired,
                Type = param.Type.ToString(),
                param.DefaultValue,
                PossibleValues = param.PossibleValues
            }).ToList()
        });

        return Results.Ok(pluginList);
    }

    /// <summary>
    /// Gets details for a specific plugin (admin only).
    /// </summary>
    /// <param name="name">The plugin name.</param>
    /// <param name="plugins">The job trigger plugins.</param>
    /// <returns>The plugin details.</returns>
    private static IResult GetPlugin(
        string name,
        [FromServices] IEnumerable<IJobTriggerPlugin> plugins)
    {
        var plugin = plugins.FirstOrDefault(p => p.JobName == name);
        if (plugin == null)
        {
            return Results.NotFound($"Plugin '{name}' not found.");
        }

        return Results.Ok(new
        {
            plugin.JobName,
            RequiredRoles = plugin.RequiredRoles.ToList(),
            Parameters = plugin.Parameters.Select(param => new
            {
                param.Name,
                param.DisplayName,
                param.Description,
                param.IsRequired,
                Type = param.Type.ToString(),
                param.DefaultValue,
                PossibleValues = param.PossibleValues
            }).ToList()
        });
    }

    /// <summary>
    /// Gets job history for all jobs the user has access to.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The job history.</returns>
    /// <remarks>
    /// In a real application, this would fetch from a database.
    /// This is a placeholder to demonstrate the API structure.
    /// </remarks>
    private static IResult GetJobHistory(ClaimsPrincipal user)
    {
        // In a real application, this would fetch from a database
        // This is a placeholder for demonstration purposes
        return Results.Ok(new object[]
        {
            new {
                JobName = "SampleJob",
                ExecutedAt = DateTime.UtcNow.AddDays(-1),
                ExecutedBy = user.FindFirstValue(ClaimTypes.NameIdentifier),
                Success = true,
                Parameters = new Dictionary<string, string>
                {
                    ["environment"] = "Development",
                    ["version"] = "1.0.0",
                    ["skipTests"] = "false"
                }
            },
            new {
                JobName = "AdvancedDeployment",
                ExecutedAt = DateTime.UtcNow.AddDays(-2),
                ExecutedBy = user.FindFirstValue(ClaimTypes.NameIdentifier),
                Success = true,
                Parameters = new Dictionary<string, string>
                {
                    ["environment"] = "QA",
                    ["version"] = "1.0.0",
                    ["notifyUsers"] = "true"
                }
            }
        });
    }

    /// <summary>
    /// Gets job history for a specific job.
    /// </summary>
    /// <param name="name">The job name.</param>
    /// <param name="user">The user.</param>
    /// <returns>The job history.</returns>
    /// <remarks>
    /// In a real application, this would fetch from a database.
    /// This is a placeholder to demonstrate the API structure.
    /// </remarks>
    private static IResult GetJobHistoryByName(string name, ClaimsPrincipal user)
    {
        // In a real application, this would fetch from a database
        // This is a placeholder for demonstration purposes
        if (name == "SampleJob")
        {
            return Results.Ok(new object[]
            {
                new {
                    JobName = "SampleJob",
                    ExecutedAt = DateTime.UtcNow.AddDays(-1),
                    ExecutedBy = user.FindFirstValue(ClaimTypes.NameIdentifier),
                    Success = true,
                    Parameters = new Dictionary<string, string>
                    {
                        ["environment"] = "Development",
                        ["version"] = "1.0.0",
                        ["skipTests"] = "false"
                    }
                },
                new {
                    JobName = "SampleJob",
                    ExecutedAt = DateTime.UtcNow.AddDays(-3),
                    ExecutedBy = user.FindFirstValue(ClaimTypes.NameIdentifier),
                    Success = false,
                    Parameters = new Dictionary<string, string>
                    {
                        ["environment"] = "Production",
                        ["version"] = "0.9.0",
                        ["skipTests"] = "true"
                    },
                    ErrorMessage = "Deployment to Production failed."
                }
            });
        }
        else if (name == "AdvancedDeployment")
        {
            return Results.Ok(new object[]
            {
                new {
                    JobName = "AdvancedDeployment",
                    ExecutedAt = DateTime.UtcNow.AddDays(-2),
                    ExecutedBy = user.FindFirstValue(ClaimTypes.NameIdentifier),
                    Success = true,
                    Parameters = new Dictionary<string, string>
                    {
                        ["environment"] = "QA",
                        ["version"] = "1.0.0",
                        ["notifyUsers"] = "true"
                    }
                }
            });
        }
        else
        {
            return Results.NotFound($"No history found for job '{name}'.");
        }
    }
}

/// <summary>
/// Extension methods for authorization policy builders.
/// </summary>
internal static class AuthorizationPolicyBuilderExtensions
{
    /// <summary>
    /// Requires job access for the job name in the route.
    /// </summary>
    /// <param name="builder">The authorization policy builder.</param>
    /// <returns>The authorization policy builder.</returns>
    public static AuthorizationPolicyBuilder RequireJobAccess(this AuthorizationPolicyBuilder builder)
    {
        return builder.AddRequirements(new JobAccessFromRouteRequirement());
    }

    /// <summary>
    /// Requires the specified role.
    /// </summary>
    /// <param name="builder">The authorization policy builder.</param>
    /// <param name="role">The role name.</param>
    /// <returns>The authorization policy builder.</returns>
    public static AuthorizationPolicyBuilder RequireRole(this AuthorizationPolicyBuilder builder, string role)
    {
        return builder.RequireRole(new[] { role });
    }
}
