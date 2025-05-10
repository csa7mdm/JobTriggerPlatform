namespace JobTriggerPlatform.Application.Abstractions
{
    /// <summary>
    /// Represents a parameter for a job trigger.
    /// </summary>
    public class JobTriggerParameter
    {
        /// <summary>
        /// Gets or sets the name of the parameter.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the display name of the parameter.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter is required.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets the description of the parameter.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the default value of the parameter.
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the type of the parameter.
        /// </summary>
        public ParameterType Type { get; set; } = ParameterType.String;

        /// <summary>
        /// Gets or sets the possible values for a Select parameter type.
        /// </summary>
        public IEnumerable<string>? PossibleValues { get; set; }
    }
}