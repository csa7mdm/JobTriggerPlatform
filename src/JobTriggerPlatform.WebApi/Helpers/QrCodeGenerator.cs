using QRCoder;

namespace JobTriggerPlatform.WebApi.Helpers;

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
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);
        
        return Convert.ToBase64String(qrCodeImage);
    }
}
