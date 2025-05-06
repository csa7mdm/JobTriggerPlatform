using FluentValidation;
using System.Collections.Generic;
using System.Linq;

namespace JobTriggerPlatform.WebApi.Models;

/// <summary>
/// Validator for <see cref="JobTriggerRequest"/>.
/// </summary>
public class JobTriggerRequestValidator : AbstractValidator<JobTriggerRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobTriggerRequestValidator"/> class.
    /// </summary>
    public JobTriggerRequestValidator()
    {
        // Basic validation rules that don't require the plugin
        RuleFor(x => x.Parameters)
            .NotNull()
            .WithMessage("Parameters are required.");
    }
}
