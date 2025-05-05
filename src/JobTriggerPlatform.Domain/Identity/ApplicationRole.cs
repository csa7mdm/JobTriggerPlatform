using Microsoft.AspNetCore.Identity;

namespace JobTriggerPlatform.Domain.Identity;

/// <summary>
/// Represents a role in the application.
/// Extends the ASP.NET Core Identity IdentityRole class.
/// </summary>
public class ApplicationRole : IdentityRole
{
    /// <summary>
    /// Gets or sets the description of the role.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the date when the role was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationRole"/> class.
    /// </summary>
    public ApplicationRole() : base()
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationRole"/> class.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    public ApplicationRole(string roleName) : base(roleName)
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationRole"/> class.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    /// <param name="description">The description of the role.</param>
    public ApplicationRole(string roleName, string? description) : base(roleName)
    {
        Description = description;
    }
}
