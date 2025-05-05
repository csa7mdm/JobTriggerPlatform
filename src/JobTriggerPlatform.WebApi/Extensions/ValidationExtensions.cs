using FluentValidation.Results;

namespace JobTriggerPlatform.WebApi.Extensions;

/// <summary>
/// Extension methods for validation.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Converts validation results to a dictionary for use with ValidationProblem.
    /// </summary>
    /// <param name="validationResult">The validation result.</param>
    /// <returns>A dictionary of error messages.</returns>
    public static IDictionary<string, string[]> ToDictionary(this ValidationResult validationResult)
    {
        return validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );
    }
}
