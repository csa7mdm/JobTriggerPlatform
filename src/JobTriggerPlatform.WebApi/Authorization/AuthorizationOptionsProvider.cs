using JobTriggerPlatform.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace JobTriggerPlatform.WebApi.Authorization;

/// <summary>
/// Provider for authorization options.
/// </summary>
public class AuthorizationOptionsProvider : IAuthorizationOptionsProvider
{
    private readonly IOptions<AuthorizationOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationOptionsProvider"/> class.
    /// </summary>
    /// <param name="options">The authorization options.</param>
    public AuthorizationOptionsProvider(IOptions<AuthorizationOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc/>
    public void AddJobAccessPolicy(string jobName)
    {
        _options.Value.AddJobAccessPolicy(jobName);
    }
}
