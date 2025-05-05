using JobTriggerPlatform.Domain.Identity;
using JobTriggerPlatform.WebApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JobTriggerPlatform.WebApi.Controllers;

/// <summary>
/// Controller for managing two-factor authentication.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TwoFactorController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TwoFactorController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TwoFactorController"/> class.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="logger">The logger.</param>
    public TwoFactorController(UserManager<ApplicationUser> userManager, ILogger<TwoFactorController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current status of two-factor authentication for the user.
    /// </summary>
    /// <returns>The 2FA status.</returns>
    [HttpGet("status")]
    public async Task<IActionResult> GetTwoFactorStatus()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        return Ok(new
        {
            IsEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
            IsMachineRemembered = await _userManager.IsTwoFactorClientRememberedAsync(user)
        });
    }

    /// <summary>
    /// Gets the authenticator key and QR code for setting up 2FA.
    /// </summary>
    /// <returns>The authenticator key and QR code.</returns>
    [HttpGet("authenticator-key")]
    public async Task<IActionResult> GetAuthenticatorKey()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        // Reset the authenticator key if it doesn't exist
        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        // Generate the QR code URI
        var authenticatorUri = GenerateQrCodeUri(user.Email, unformattedKey);
        var qrCodeBase64 = QrCodeGenerator.GenerateQrCodeAsBase64(authenticatorUri);

        return Ok(new
        {
            AuthenticatorKey = unformattedKey,
            QrCodeUri = authenticatorUri,
            QrCodeBase64 = $"data:image/png;base64,{qrCodeBase64}"
        });
    }

    /// <summary>
    /// Enables two-factor authentication for the user.
    /// </summary>
    /// <param name="model">The verification model.</param>
    /// <returns>The result of the operation.</returns>
    [HttpPost("enable")]
    public async Task<IActionResult> EnableTwoFactorAuthentication([FromBody] VerifyModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        var isTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, model.Code);

        if (!isTokenValid)
        {
            return BadRequest("Verification code is invalid.");
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        
        // Generate recovery codes
        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        _logger.LogInformation("User {UserId} has enabled 2FA with an authenticator app", user.Id);

        return Ok(new
        {
            RecoveryCodes = recoveryCodes,
            Message = "Two-factor authentication has been enabled."
        });
    }

    /// <summary>
    /// Disables two-factor authentication for the user.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    [HttpPost("disable")]
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
            return BadRequest(result.Errors);
        }

        _logger.LogInformation("User {UserId} has disabled 2FA", user.Id);

        return Ok(new
        {
            Message = "Two-factor authentication has been disabled."
        });
    }

    /// <summary>
    /// Generates new recovery codes for the user.
    /// </summary>
    /// <returns>The new recovery codes.</returns>
    [HttpPost("generate-recovery-codes")]
    public async Task<IActionResult> GenerateRecoveryCodes()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        if (!await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return BadRequest("Cannot generate recovery codes for user with 2FA disabled.");
        }

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        _logger.LogInformation("User {UserId} has generated new 2FA recovery codes", user.Id);

        return Ok(new
        {
            RecoveryCodes = recoveryCodes
        });
    }

    /// <summary>
    /// Resets the authenticator key for the user.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    [HttpPost("reset-authenticator")]
    public async Task<IActionResult> ResetAuthenticator()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);

        _logger.LogInformation("User {UserId} has reset their authentication app key", user.Id);

        return Ok(new
        {
            Message = "Authenticator app key has been reset. You will need to configure your authenticator app using the new key."
        });
    }

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
/// Model for verifying a code.
/// </summary>
public class VerifyModel
{
    /// <summary>
    /// Gets or sets the verification code.
    /// </summary>
    [Required]
    public string Code { get; set; } = string.Empty;
}
