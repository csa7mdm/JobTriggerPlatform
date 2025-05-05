using FluentValidation;
using JobTriggerPlatform.Application.Abstractions;
using JobTriggerPlatform.Infrastructure.Authorization;
using JobTriggerPlatform.WebApi.Extensions;
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
    /// <returns>The web application.</returns>
    public static WebApplication MapJobEndpoints(this WebApplication app)
    {
        var jobsGroup = app.MapGroup("/api/jobs")
            .WithTags("Jobs")
            .WithOpenApi()
            .WithJobsOpenApi();

        // Get all available jobs
        jobsGroup.MapGet("/", GetJobs)
            .WithName("GetJobs")
            .RequireAuthorization()
            .WithGetJobsOpenApi();

        // Get job details
        jobsGroup.MapGet("/{name}", GetJob)
            .WithName("GetJob")
            .RequireAuthorization(policy => policy.RequireJobAccess())
            .WithGetJobOpenApi();

        // Trigger a job
        jobsGroup.MapPost("/{name}", TriggerJob)
            .WithName("TriggerJob")
            .RequireAuthorization(policy => policy.RequireJobAccess())
            .Accepts<JobTriggerRequest>("application/json")
            .WithTriggerJobOpenApi();

        // Get all plugins
        jobsGroup.MapGet("/plugins", GetPlugins)
            .WithName("GetPlugins")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // Get plugin details
        jobsGroup.MapGet("/plugins/{name}", GetPlugin)
            .WithName("GetPlugin")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // Get job history
        jobsGroup.MapGet("/history", GetJobHistory)
            .WithName("GetJobHistory")
            .RequireAuthorization();

        // Get job history for a specific job
        jobsGroup.MapGet("/history/{name}", GetJobHistoryByName)
            .WithName("GetJobHistoryByName")
            .RequireAuthorization(policy => policy.RequireJobAccess());

        return app;
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
        [FromServices] ILogger<JobEndpoints> logger)
    {
        var plugin = plugins.FirstOrDefault(p => p.JobName == name);
        if (plugin == null)
        {
            return Results.NotFound($"Job '{name}' not found.");
        }

        try
        {
            // Create and run the validator for the request
            var validator = new JobTriggerRequestValidator(plugin);
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
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
        return Results.Ok(new[]
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
            return Results.Ok(new[]
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
            return Results.Ok(new[]
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
        return builder.RequireRole(role);
    }
}
