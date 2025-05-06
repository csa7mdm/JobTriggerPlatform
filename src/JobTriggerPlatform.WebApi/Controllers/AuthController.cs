using JobTriggerPlatform.Application.Interfaces;
using JobTriggerPlatform.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JobTriggerPlatform.WebApi.Controllers;

/// <summary>
/// Controller for authentication operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="signInManager">The sign-in manager.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="emailService">The email service.</param>
    /// <param name="logger">The logger.</param>
    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        IEmailService emailService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="model">The registration model.</param>
    /// <returns>The result of the registration.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            return BadRequest("User with this email already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        // Add the user to the specified role
        if (!string.IsNullOrEmpty(model.Role))
        {
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        // Generate email confirmation token and send email
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = Url.Action("ConfirmEmail", "Auth", new { userId = user.Id, token }, Request.Scheme);

        var emailBody = $@"
            <h2>Welcome to the Job Trigger Platform!</h2>
            <p>Please confirm your email by clicking the link below:</p>
            <p><a href='{confirmationLink}'>Confirm Email</a></p>
            <p>If you didn't register for the Job Trigger Platform, you can ignore this email.</p>
        ";

        await _emailService.SendEmailAsync(user.Email, "Confirm your email", emailBody);

        return Ok("User registered successfully. Please check your email to confirm your account.");
    }

    /// <summary>
    /// Confirms a user's email.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="token">The confirmation token.</param>
    /// <returns>The result of the email confirmation.</returns>
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            return BadRequest("User ID and token are required.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            return BadRequest("Failed to confirm email.");
        }

        return Redirect($"{_configuration["ClientUrl"]}/login?emailConfirmed=true");
    }

    /// <summary>
    /// Logs in a user.
    /// </summary>
    /// <param name="model">The login model.</param>
    /// <returns>The result of the login attempt.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return Unauthorized("Invalid credentials.");
        }

        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            return Unauthorized("Email not confirmed. Please check your email for the confirmation link.");
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var token = await GenerateJwtToken(user);
            return Ok(new { Token = token });
        }

        if (result.RequiresTwoFactor)
        {
            return Ok(new { RequiresTwoFactor = true, Provider = "Authenticator" });
        }

        if (result.IsLockedOut)
        {
            return Unauthorized("Account locked out. Please try again later.");
        }

        return Unauthorized("Invalid credentials.");
    }

    /// <summary>
    /// Logs in a user with two-factor authentication.
    /// </summary>
    /// <param name="model">The two-factor authentication model.</param>
    /// <returns>The result of the login attempt.</returns>
    [HttpPost("login-2fa")]
    public async Task<IActionResult> LoginWithTwoFactor([FromBody] TwoFactorModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            return Unauthorized("Invalid two-factor authentication attempt.");
        }

        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(model.Code, model.RememberMe, model.RememberClient);

        if (result.Succeeded)
        {
            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var token = await GenerateJwtToken(user);
            return Ok(new { Token = token });
        }

        if (result.IsLockedOut)
        {
            return Unauthorized("Account locked out. Please try again later.");
        }

        return Unauthorized("Invalid authenticator code.");
    }

    /// <summary>
    /// Enables two-factor authentication for a user.
    /// </summary>
    /// <returns>The QR code URI for setting up 2FA.</returns>
    [HttpPost("enable-2fa")]
    public async Task<IActionResult> EnableTwoFactorAuthentication()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var qrCodeUri = GenerateQrCodeUri(user.Email, unformattedKey);

        return Ok(new
        {
            AuthenticatorKey = unformattedKey,
            QrCodeUri = qrCodeUri
        });
    }

    /// <summary>
    /// Verifies and completes the setup of two-factor authentication.
    /// </summary>
    /// <param name="model">The verify 2FA model.</param>
    /// <returns>The result of the verification.</returns>
    [HttpPost("verify-2fa")]
    public async Task<IActionResult> VerifyTwoFactorAuthentication([FromBody] VerifyTwoFactorModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, model.Code);

        if (!is2faTokenValid)
        {
            return BadRequest("Verification code is invalid.");
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);

        // Generate recovery codes
        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        return Ok(new { RecoveryCodes = recoveryCodes });
    }

    /// <summary>
    /// Disables two-factor authentication for a user.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    [HttpPost("disable-2fa")]
    public async Task<IActionResult> DisableTwoFactorAuthentication()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!result.Succeeded)
        {
            return BadRequest("Failed to disable two-factor authentication.");
        }

        return Ok("Two-factor authentication has been disabled.");
    }

    /// <summary>
    /// Generates a JWT token for the specified user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The JWT token.</returns>
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

    /// <summary>
    /// Generates a QR code URI for the authenticator app.
    /// </summary>
    /// <param name="email">The user's email.</param>
    /// <param name="unformattedKey">The unformatted key.</param>
    /// <returns>The QR code URI.</returns>
    private string GenerateQrCodeUri(string email, string unformattedKey)
    {
        const string authenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        return string.Format(
            authenticatorUriFormat,
            Uri.EscapeDataString("JobTriggerPlatform"),
            Uri.EscapeDataString(email),
            unformattedKey);
    }
}

/// <summary>
/// Model for user registration.
/// </summary>
public class RegisterModel
{
    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the first name.
    /// </summary>
    [Required]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last name.
    /// </summary>
    [Required]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role.
    /// </summary>
    public string? Role { get; set; }
}

/// <summary>
/// Model for user login.
/// </summary>
public class LoginModel
{
    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to remember the login.
    /// </summary>
    public bool RememberMe { get; set; }
}

/// <summary>
/// Model for two-factor authentication.
/// </summary>
public class TwoFactorModel
{
    /// <summary>
    /// Gets or sets the authenticator code.
    /// </summary>
    [Required]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to remember the login.
    /// </summary>
    public bool RememberMe { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to remember the client.
    /// </summary>
    public bool RememberClient { get; set; }
}

/// <summary>
/// Model for verifying two-factor authentication setup.
/// </summary>
public class VerifyTwoFactorModel
{
    /// <summary>
    /// Gets or sets the authenticator code.
    /// </summary>
    [Required]
    public string Code { get; set; } = string.Empty;
}
