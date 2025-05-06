using JobTriggerPlatform.Application.Abstractions;
using JobTriggerPlatform.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace JobTriggerPlatform.WebApi.Controllers;

/// <summary>
/// Controller for managing roles and permissions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEnumerable<IJobTriggerPlugin> _plugins;
    private readonly ILogger<RolesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RolesController"/> class.
    /// </summary>
    /// <param name="roleManager">The role manager.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="plugins">The job trigger plugins.</param>
    /// <param name="logger">The logger.</param>
    public RolesController(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IEnumerable<IJobTriggerPlugin> plugins,
        ILogger<RolesController> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _plugins = plugins;
        _logger = logger;
    }

    /// <summary>
    /// Gets all available roles.
    /// </summary>
    /// <returns>The list of roles.</returns>
    [HttpGet]
    public IActionResult GetRoles()
    {
        var roles = _roleManager.Roles
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.CreatedAt
            });

        return Ok(roles);
    }

    /// <summary>
    /// Creates a new role.
    /// </summary>
    /// <param name="model">The role creation model.</param>
    /// <returns>The result of the operation.</returns>
    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] RoleModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (await _roleManager.RoleExistsAsync(model.Name))
        {
            return BadRequest("Role already exists.");
        }

        var role = new ApplicationRole(model.Name, model.Description);
        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return CreatedAtAction(nameof(GetRoles), new { id = role.Id }, new
        {
            role.Id,
            role.Name,
            role.Description,
            role.CreatedAt
        });
    }

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    /// <param name="id">The role ID.</param>
    /// <param name="model">The role update model.</param>
    /// <returns>The result of the operation.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] RoleModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound("Role not found.");
        }

        role.Name = model.Name;
        role.Description = model.Description;

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new
        {
            role.Id,
            role.Name,
            role.Description,
            role.CreatedAt
        });
    }

    /// <summary>
    /// Deletes a role.
    /// </summary>
    /// <param name="id">The role ID.</param>
    /// <returns>The result of the operation.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound("Role not found.");
        }

        // Check if role is one of the predefined roles
        if (role.Name == "Admin" || role.Name == "Dev" || role.Name == "QA")
        {
            return BadRequest("Cannot delete predefined roles.");
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return NoContent();
    }

    /// <summary>
    /// Gets all users with their roles.
    /// </summary>
    /// <returns>The list of users with their roles.</returns>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsersWithRoles()
    {
        var users = _userManager.Users.ToList();
        var userRoles = new List<object>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRoles.Add(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.CreatedAt,
                user.LastLogin,
                Roles = roles
            });
        }

        return Ok(userRoles);
    }

    /// <summary>
    /// Updates a user's roles.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="model">The role assignment model.</param>
    /// <returns>The result of the operation.</returns>
    [HttpPut("users/{userId}/roles")]
    public async Task<IActionResult> UpdateUserRoles(string userId, [FromBody] UserRolesModel model)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var result = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        result = await _userManager.AddToRolesAsync(user, model.Roles);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            Roles = model.Roles
        });
    }

    /// <summary>
    /// Gets all job trigger plugins with their required roles.
    /// </summary>
    /// <returns>The list of plugins and their required roles.</returns>
    [HttpGet("plugins")]
    public IActionResult GetPlugins()
    {
        var plugins = _plugins.Select(p => new
        {
            p.JobName,
            RequiredRoles = p.RequiredRoles
        });

        return Ok(plugins);
    }
}

/// <summary>
/// Model for role creation and update.
/// </summary>
public class RoleModel
{
    /// <summary>
    /// Gets or sets the name of the role.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the role.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Model for user role assignment.
/// </summary>
public class UserRolesModel
{
    /// <summary>
    /// Gets or sets the roles to assign to the user.
    /// </summary>
    [Required]
    public IList<string> Roles { get; set; } = new List<string>();
}
