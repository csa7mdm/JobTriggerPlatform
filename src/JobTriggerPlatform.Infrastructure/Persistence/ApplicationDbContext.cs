using JobTriggerPlatform.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JobTriggerPlatform.Infrastructure.Persistence;

/// <summary>
/// The main database context for the application.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Identity table schemas
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users", "identity");
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("Roles", "identity");
        });

        builder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("UserRoles", "identity");
            entity.HasKey(e => new { e.UserId, e.RoleId });
        });

        builder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("UserClaims", "identity");
            entity.HasKey(e => e.Id);
        });

        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("UserLogins", "identity");
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });
        });

        builder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("RoleClaims", "identity");
            entity.HasKey(e => e.Id);
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("UserTokens", "identity");
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });
        });
    }
}
