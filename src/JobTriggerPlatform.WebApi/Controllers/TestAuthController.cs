using JobTriggerPlatform.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JobTriggerPlatform.WebApi.Controllers;

/// <summary>
/// Test authentication controller for development environments only.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class TestAuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TestAuthController> _logger;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestAuthController"/> class.
    /// </summary>
    public TestAuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        ILogger<TestAuthController> logger,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Simple login endpoint for testing purposes.
    /// </summary>
    /// <param name="role">The role to login as (admin, operator, or viewer).</param>
    /// <returns>A JWT token for the test user.</returns>
    [HttpGet("login/{role}")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login(string role)
    {
        // Only allow this in development
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        // Normalize the role
        role = role.ToLower();
        
        // Map to a valid role
        string roleName;
        switch (role)
        {
            case "admin":
                roleName = "Admin";
                break;
            case "operator":
                roleName = "Operator";
                break;
            case "viewer":
                roleName = "Viewer";
                break;
            default:
                return BadRequest("Invalid role. Use 'admin', 'operator', or 'viewer'.");
        }

        // Ensure the role exists
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new ApplicationRole(roleName, $"{roleName} role"));
        }

        // Create a user email based on role
        var email = $"{role}@example.com";
        
        // Check if the user exists
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Create the user
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = $"Test {roleName} User"
            };

            // Use a simple password - this is development only
            var result = await _userManager.CreateAsync(user, "Password123!");
            if (!result.Succeeded)
            {
                return BadRequest($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // Assign the role
            await _userManager.AddToRoleAsync(user, roleName);
        }

        // Generate a token
        var token = await GenerateJwtToken(user);

        // Return the token
        return Ok(new { Token = token });
    }

    /// <summary>
    /// Generates a JWT token for the specified user.
    /// </summary>
    private async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        var userRoles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT:Secret is not configured")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(
            _configuration["JWT:ExpiryInMinutes"] ?? "60"));

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:Issuer"],
            audience: _configuration["JWT:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
