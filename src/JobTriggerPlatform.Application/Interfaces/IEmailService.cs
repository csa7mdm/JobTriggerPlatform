namespace JobTriggerPlatform.Application.Interfaces
{
    /// <summary>
    /// Interface for email service.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email to a single recipient.
        /// </summary>
        /// <param name="to">The recipient email address.</param>
        /// <param name="subject">The email subject.</param>
        /// <param name="body">The email body.</param>
        /// <param name="isHtml">Indicates whether the body contains HTML.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an email to multiple recipients.
        /// </summary>
        /// <param name="to">The recipient email addresses.</param>
        /// <param name="subject">The email subject.</param>
        /// <param name="body">The email body.</param>
        /// <param name="isHtml">Indicates whether the body contains HTML.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
    }
}