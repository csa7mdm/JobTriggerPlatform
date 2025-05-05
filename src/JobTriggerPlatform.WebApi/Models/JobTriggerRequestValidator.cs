using FluentValidation;
using JobTriggerPlatform.Application.Abstractions;

namespace JobTriggerPlatform.WebApi.Models;

/// <summary>
/// Validator for <see cref="JobTriggerRequest"/>.
/// </summary>
public class JobTriggerRequestValidator : AbstractValidator<JobTriggerRequest>
{
    private readonly IJobTriggerPlugin _plugin;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobTriggerRequestValidator"/> class.
    /// </summary>
    /// <param name="plugin">The job trigger plugin.</param>
    public JobTriggerRequestValidator(IJobTriggerPlugin plugin)
    {
        _plugin = plugin;

        RuleFor(x => x.Parameters)
            .NotNull()
            .WithMessage("Parameters are required.");

        // Validate that all required parameters are present
        foreach (var parameter in _plugin.Parameters.Where(p => p.IsRequired))
        {
            RuleFor(x => x.Parameters)
                .Must(parameters => parameters.ContainsKey(parameter.Name) && !string.IsNullOrEmpty(parameters[parameter.Name]))
                .WithMessage($"Parameter '{parameter.Name}' is required.");
        }

        // Validate parameters with specific types
        foreach (var parameter in _plugin.Parameters)
        {
            if (parameter.Type == ParameterType.Number)
            {
                RuleFor(x => x.Parameters)
                    .Must(parameters => !parameters.ContainsKey(parameter.Name) || 
                                       string.IsNullOrEmpty(parameters[parameter.Name]) || 
                                       decimal.TryParse(parameters[parameter.Name], out _))
                    .WithMessage($"Parameter '{parameter.Name}' must be a valid number.");
            }
            else if (parameter.Type == ParameterType.Boolean)
            {
                RuleFor(x => x.Parameters)
                    .Must(parameters => !parameters.ContainsKey(parameter.Name) || 
                                       string.IsNullOrEmpty(parameters[parameter.Name]) || 
                                       bool.TryParse(parameters[parameter.Name], out _))
                    .WithMessage($"Parameter '{parameter.Name}' must be a valid boolean (true/false).");
            }
            else if (parameter.Type == ParameterType.Date)
            {
                RuleFor(x => x.Parameters)
                    .Must(parameters => !parameters.ContainsKey(parameter.Name) || 
                                       string.IsNullOrEmpty(parameters[parameter.Name]) || 
                                       DateTime.TryParse(parameters[parameter.Name], out _))
                    .WithMessage($"Parameter '{parameter.Name}' must be a valid date.");
            }
            else if (parameter.Type == ParameterType.Select && parameter.PossibleValues != null)
            {
                RuleFor(x => x.Parameters)
                    .Must(parameters => !parameters.ContainsKey(parameter.Name) || 
                                       string.IsNullOrEmpty(parameters[parameter.Name]) || 
                                       parameter.PossibleValues.Contains(parameters[parameter.Name]))
                    .WithMessage($"Parameter '{parameter.Name}' must be one of: {string.Join(", ", parameter.PossibleValues)}.");
            }
        }
    }
}
