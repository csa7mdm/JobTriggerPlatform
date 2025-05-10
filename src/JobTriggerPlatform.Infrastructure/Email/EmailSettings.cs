namespace JobTriggerPlatform.Infrastructure.Email
{
    /// <summary>
    /// Settings for email service.
    /// </summary>
    public class EmailSettings
    {
        /// <summary>
        /// Gets or sets the SMTP host.
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SMTP port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the sender email.
        /// </summary>
        public string SenderEmail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sender name.
        /// </summary>
        public string SenderName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether SSL is enabled.
        /// </summary>
        public bool EnableSsl { get; set; }
    }
}
