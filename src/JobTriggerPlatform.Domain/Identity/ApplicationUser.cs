using Microsoft.AspNetCore.Identity;

namespace JobTriggerPlatform.Domain.Identity;

/// <summary>
/// Represents a user in the application.
/// Extends the ASP.NET Core Identity IdentityUser class.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Gets or sets the full name of the user.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Gets or sets the date when the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether the user has completed initial setup.
    /// </summary>
    public bool HasCompletedSetup { get; set; }

    /// <summary>
    /// Gets or sets the date when the user last logged in.
    /// </summary>
    public DateTime? LastLogin { get; set; }
}
