namespace JobTriggerPlatform.Application.Abstractions;

/// <summary>
/// Represents a parameter that a plugin requires.
/// </summary>
public class PluginParameter
{
    /// <summary>
    /// Gets or sets the name of the parameter.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the display name of the parameter.
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description of the parameter.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the parameter is required.
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Gets or sets the type of the parameter.
    /// </summary>
    public ParameterType Type { get; set; } = ParameterType.String;

    /// <summary>
    /// Gets or sets the default value of the parameter.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the possible values for the parameter, if applicable.
    /// </summary>
    public IReadOnlyCollection<string>? PossibleValues { get; set; }
}

/// <summary>
/// Specifies the type of a plugin parameter.
/// </summary>
public enum ParameterType
{
    /// <summary>
    /// A string parameter.
    /// </summary>
    String,

    /// <summary>
    /// A numeric parameter.
    /// </summary>
    Number,

    /// <summary>
    /// A boolean parameter.
    /// </summary>
    Boolean,

    /// <summary>
    /// A date parameter.
    /// </summary>
    Date,

    /// <summary>
    /// A select parameter (dropdown).
    /// </summary>
    Select,

    /// <summary>
    /// A multi-select parameter.
    /// </summary>
    MultiSelect,

    /// <summary>
    /// A password parameter.
    /// </summary>
    Password,

    /// <summary>
    /// A file parameter.
    /// </summary>
    File
}
