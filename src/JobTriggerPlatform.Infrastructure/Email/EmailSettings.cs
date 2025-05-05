namespace JobTriggerPlatform.Infrastructure.Email;

/// <summary>
/// Settings for the email service.
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Gets or sets the SMTP server host.
    /// </summary>
    public string Host { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the SMTP server port.
    /// </summary>
    public int Port { get; set; }
    
    /// <summary>
    /// Gets or sets the sender email address.
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the sender display name.
    /// </summary>
    public string SenderName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the username for SMTP authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the password for SMTP authentication.
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets a value indicating whether to use SSL.
    /// </summary>
    public bool EnableSsl { get; set; } = true;
}
