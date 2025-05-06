using JobTriggerPlatform.Application.Abstractions;
using JobTriggerPlatform.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace JobTriggerPlatform.WebApi.Controllers;

/// <summary>
/// Controller for managing job access.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class JobAccessController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEnumerable<IJobTriggerPlugin> _plugins;
    private readonly ILogger<JobAccessController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobAccessController"/> class.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="plugins">The job trigger plugins.</param>
    /// <param name="logger">The logger.</param>
    public JobAccessController(
        UserManager<ApplicationUser> userManager,
        IEnumerable<IJobTriggerPlugin> plugins,
        ILogger<JobAccessController> logger)
    {
        _userManager = userManager;
        _plugins = plugins;
        _logger = logger;
    }

    /// <summary>
    /// Gets all users with their job access.
    /// </summary>
    /// <returns>The list of users with their job access.</returns>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsersWithJobAccess()
    {
        var users = _userManager.Users.ToList();
        var result = new List<object>();

        foreach (var user in users)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var jobAccessClaims = claims.Where(c => c.Type == "JobAccess").Select(c => c.Value).ToList();

            result.Add(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                JobAccess = jobAccessClaims
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets job access for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The user's job access.</returns>
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserJobAccess(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var claims = await _userManager.GetClaimsAsync(user);
        var jobAccessClaims = claims.Where(c => c.Type == "JobAccess").Select(c => c.Value).ToList();
        var roles = await _userManager.GetRolesAsync(user);

        // Get jobs accessible through roles
        var accessibleJobsByRole = _plugins
            .Where(p => p.RequiredRoles.Any(role => roles.Contains(role)))
            .Select(p => p.JobName)
            .ToList();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            DirectJobAccess = jobAccessClaims,
            RoleBasedJobAccess = accessibleJobsByRole
        });
    }

    /// <summary>
    /// Updates job access for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="model">The job access update model.</param>
    /// <returns>The result of the operation.</returns>
    [HttpPut("users/{userId}")]
    public async Task<IActionResult> UpdateUserJobAccess(string userId, [FromBody] JobAccessUpdateModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        // Verify that all job names are valid
        var invalidJobNames = model.JobAccess
            .Where(jobName => !_plugins.Any(p => p.JobName == jobName))
            .ToList();

        if (invalidJobNames.Any())
        {
            return BadRequest($"Invalid job names: {string.Join(", ", invalidJobNames)}");
        }

        // Get existing job access claims
        var claims = await _userManager.GetClaimsAsync(user);
        var existingJobAccessClaims = claims.Where(c => c.Type == "JobAccess").ToList();

        // Remove old claims
        foreach (var claim in existingJobAccessClaims)
        {
            await _userManager.RemoveClaimAsync(user, claim);
        }

        // Add new claims
        foreach (var jobName in model.JobAccess)
        {
            await _userManager.AddClaimAsync(user, new Claim("JobAccess", jobName));
        }

        _logger.LogInformation("Updated job access for user {UserId}. Added: {AddedJobs}",
            userId, string.Join(", ", model.JobAccess));

        return Ok(new
        {
            user.Id,
            user.Email,
            JobAccess = model.JobAccess
        });
    }

    /// <summary>
    /// Gets all available jobs.
    /// </summary>
    /// <returns>The list of available jobs.</returns>
    [HttpGet("jobs")]
    public IActionResult GetAvailableJobs()
    {
        var jobs = _plugins.Select(p => new
        {
            p.JobName,
            RequiredRoles = p.RequiredRoles
        });

        return Ok(jobs);
    }

    /// <summary>
    /// Adds specific job access to a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="jobName">The job name.</param>
    /// <returns>The result of the operation.</returns>
    [HttpPost("users/{userId}/jobs/{jobName}")]
    public async Task<IActionResult> AddJobAccess(string userId, string jobName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var plugin = _plugins.FirstOrDefault(p => p.JobName == jobName);
        if (plugin == null)
        {
            return NotFound($"Job '{jobName}' not found.");
        }

        var claims = await _userManager.GetClaimsAsync(user);
        if (claims.Any(c => c.Type == "JobAccess" && c.Value == jobName))
        {
            return BadRequest($"User already has access to job '{jobName}'.");
        }

        await _userManager.AddClaimAsync(user, new Claim("JobAccess", jobName));

        _logger.LogInformation("Added job access {JobName} to user {UserId}", jobName, userId);

        return Ok(new
        {
            Message = $"Access to job '{jobName}' added successfully.",
            JobName = jobName,
            UserId = userId
        });
    }

    /// <summary>
    /// Removes specific job access from a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="jobName">The job name.</param>
    /// <returns>The result of the operation.</returns>
    [HttpDelete("users/{userId}/jobs/{jobName}")]
    public async Task<IActionResult> RemoveJobAccess(string userId, string jobName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var plugin = _plugins.FirstOrDefault(p => p.JobName == jobName);
        if (plugin == null)
        {
            return NotFound($"Job '{jobName}' not found.");
        }

        var claims = await _userManager.GetClaimsAsync(user);
        var claim = claims.FirstOrDefault(c => c.Type == "JobAccess" && c.Value == jobName);

        if (claim == null)
        {
            return BadRequest($"User does not have direct access to job '{jobName}'.");
        }

        await _userManager.RemoveClaimAsync(user, claim);

        _logger.LogInformation("Removed job access {JobName} from user {UserId}", jobName, userId);

        return Ok(new
        {
            Message = $"Access to job '{jobName}' removed successfully.",
            JobName = jobName,
            UserId = userId
        });
    }
}

/// <summary>
/// Model for updating job access.
/// </summary>
public class JobAccessUpdateModel
{
    /// <summary>
    /// Gets or sets the list of job names to grant access to.
    /// </summary>
    [Required]
    public IList<string> JobAccess { get; set; } = new List<string>();
}
