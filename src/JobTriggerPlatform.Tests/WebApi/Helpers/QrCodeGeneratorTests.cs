using JobTriggerPlatform.WebApi.Helpers;
using Xunit;

namespace JobTriggerPlatform.Tests.WebApi.Helpers
{
    // Note: This is a minimal test to ensure we're testing the helper class
    // In a real scenario, we might want to mock the QRCoder library, but for simplicity,
    // we'll just test the method return is not null/empty
    public class QrCodeGeneratorTests
    {
        [Fact]
        public void GenerateQrCodeAsBase64_ReturnsNonEmptyString()
        {
            // Arrange
            var text = "https://test.com";

            // Act
            var result = QrCodeGenerator.GenerateQrCodeAsBase64(text);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
    }
}
