namespace JobTriggerPlatform.WebApi.Helpers
{
    /// <summary>
    /// Helper class for generating QR codes.
    /// </summary>
    public static class QrCodeGenerator
    {
        /// <summary>
        /// Generates a QR code as a base64 encoded string.
        /// </summary>
        /// <param name="text">The text to encode in the QR code.</param>
        /// <returns>A base64 encoded string representing the QR code image.</returns>
        public static string GenerateQrCodeAsBase64(string text)
        {
            // In a real implementation, this would use a QR code library.
            // For testing purposes, we'll return a dummy value.
            return "DUMMY_QR_CODE_BASE64_" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
        }
    }
}