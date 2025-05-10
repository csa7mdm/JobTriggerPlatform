using JobTriggerPlatform.Domain.Identity;
using JobTriggerPlatform.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace JobTriggerPlatform.Tests.WebApi.Controllers
{
    public class TwoFactorControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly Mock<ILogger<TwoFactorController>> _mockLogger;
        private readonly TwoFactorController _controller;
        private readonly ClaimsPrincipal _user;

        public TwoFactorControllerTests()
        {
            // Setup UserManager mock
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null, null, null, null, null, null, null, null);

            // Setup SignInManager mock
            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
                null, null, null, null);

            // Setup Logger mock
            _mockLogger = new Mock<ILogger<TwoFactorController>>();

            // Create controller
            _controller = new TwoFactorController(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _mockLogger.Object);

            // Create ClaimsPrincipal for tests
            var userId = "user-id";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "test@example.com"),
                new Claim(ClaimTypes.Email, "test@example.com")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            _user = new ClaimsPrincipal(identity);

            // Set controller HttpContext
            var httpContext = new DefaultHttpContext
            {
                User = _user
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task GetTwoFactorStatus_ValidUser_ReturnsTwoFactorStatus()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user-id",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(u => u.GetUserAsync(_user))
                .ReturnsAsync(user);

            _mockUserManager.Setup(u => u.GetTwoFactorEnabledAsync(user))
                .ReturnsAsync(true);

            _mockSignInManager.Setup(s => s.IsTwoFactorClientRememberedAsync(user))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.GetTwoFactorStatus();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic status = okResult.Value;

            Assert.True((bool)status.IsEnabled);
            Assert.False((bool)status.IsMachineRemembered);
        }

        [Fact]
        public async Task GetTwoFactorStatus_NoUser_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserManager.Setup(u => u.GetUserAsync(_user))
                .ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _controller.GetTwoFactorStatus();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not found.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task GetAuthenticatorKey_ValidUser_ReturnsKeyAndQrCode()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user-id",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(u => u.GetUserAsync(_user))
                .ReturnsAsync(user);

            _mockUserManager.Setup(u => u.GetAuthenticatorKeyAsync(user))
                .ReturnsAsync("AUTHKEY123456");

            // Act
            var result = await _controller.GetAuthenticatorKey();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic keyData = okResult.Value;

            Assert.Equal("AUTHKEY123456", keyData.AuthenticatorKey);
            Assert.NotNull(keyData.QrCodeUri);
            Assert.NotNull(keyData.QrCodeBase64);
            Assert.StartsWith("data:image/png;base64,", keyData.QrCodeBase64);
        }

        [Fact]
        public async Task GetAuthenticatorKey_NoExistingKey_ResetsAndReturnsNewKey()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user-id",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(u => u.GetUserAsync(_user))
                .ReturnsAsync(user);

            // First call returns empty, second call after reset returns a key
            _mockUserManager.SetupSequence(u => u.GetAuthenticatorKeyAsync(user))
                .ReturnsAsync(string.Empty)
                .ReturnsAsync("NEWKEY123456");

            _mockUserManager.Setup(u => u.ResetAuthenticatorKeyAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.GetAuthenticatorKey();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic keyData = okResult.Value;

            Assert.Equal("NEWKEY123456", keyData.AuthenticatorKey);
            Assert.NotNull(keyData.QrCodeUri);
            Assert.NotNull(keyData.QrCodeBase64);
            
            // Verify the key was reset
            _mockUserManager.Verify(u => u.ResetAuthenticatorKeyAsync(user), Times.Once);
        }

        [Fact]
        public async Task EnableTwoFactorAuthentication_ValidCode_ReturnsSuccess()
        {
            // Arrange
            var model = new VerifyModel
            {
                Code = "123456"
            };

            var user = new ApplicationUser
            {
                Id = "user-id",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(u => u.GetUserAsync(_user))
                .ReturnsAsync(user);

            _mockUserManager.Setup(u => u.VerifyTwoFactorTokenAsync(
                    user,
                    It.IsAny<string>(),
                    model.Code))
                .ReturnsAsync(true);

            _mockUserManager.Setup(u => u.SetTwoFactorEnabledAsync(user, true))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(u => u.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))
                .ReturnsAsync(new[] { "code1", "code2", "code3" });

            // Act
            var result = await _controller.EnableTwoFactorAuthentication(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic response = okResult.Value;

            // Check if the result contains recovery codes
            var recoveryCodes = (IEnumerable<string>)response.RecoveryCodes;
            Assert.Equal(3, recoveryCodes.Count());
            
            // Check the success message
            Assert.Contains("enabled", response.Message.ToString());
        }

        [Fact]
        public async Task EnableTwoFactorAuthentication_InvalidCode_ReturnsBadRequest()
        {
            // Arrange
            var model = new VerifyModel
            {
                Code = "invalid"
            };

            var user = new ApplicationUser
            {
                Id = "user-id",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(u => u.GetUserAsync(_user))
                .ReturnsAsync(user);

            _mockUserManager.Setup(u => u.VerifyTwoFactorTokenAsync(
                    user,
                    It.IsAny<string>(),
                    model.Code))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.EnableTwoFactorAuthentication(model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Verification code is invalid.", badRequestResult.Value);
        }

        [Fact]
        public async Task DisableTwoFactorAuthentication_ValidUser_ReturnsSuccess()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user-id",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(u => u.GetUserAsync(_user))
                .ReturnsAsync(user);

            _mockUserManager.Setup(u => u.SetTwoFactorEnabledAsync(user, false))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.DisableTwoFactorAuthentication();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic response = okResult.Value;

            // Check the success message
            Assert.Contains("disabled", response.Message.ToString());

            // Verify 2FA was disabled
            _mockUserManager.Verify(u => u.SetTwoFactorEnabledAsync(user, false), Times.Once);
        }

        [Fact]
        public async Task DisableTwoFactorAuthentication_NoUser_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserManager.Setup(u => u.GetUserAsync(_user))
                .ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _controller.DisableTwoFactorAuthentication();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not found.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task DisableTwoFactorAuthentication_Error_ReturnsBadRequest()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user-id",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(u => u.GetUserAsync(_user))
                .ReturnsAsync(user);

            var identityErrors = new List<IdentityError>
            {
                new IdentityError { Code = "Error", Description = "Failed to disable 2FA" }
            };

            _mockUserManager.Setup(u => u.SetTwoFactorEnabledAsync(user, false))
                .ReturnsAsync(IdentityResult.Failed(identityErrors.ToArray()));

            // Act
            var result = await _controller.DisableTwoFactorAuthentication();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<IEnumerable<IdentityError>>(badRequestResult.Value);
            Assert.Single(errors);
            Assert.Equal("Failed to disable 2FA", errors.First().Description);
        }

        [Fact]
        public async Task GenerateRecoveryCodes_TwoFactorEnabled_ReturnsNewCodes()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user-id",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(u => u.GetUserAsync(_user))
                .ReturnsAsync(user);

            _mockUserManager.Setup(u => u.GetTwoFactorEnabledAsync(user))
                .ReturnsAsync(true);

            _mockUserManager.Setup(u => u.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))
                .ReturnsAsync(new[] { "code1", "code2", "code3" });

            // Act
            var result = await _controller.GenerateRecoveryCodes();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic response = okResult.Value;

            // Check if the result contains recovery codes
            var recoveryCodes = (IEnumerable<string>)response.RecoveryCodes;
            Assert.Equal(3, recoveryCodes.Count());
        }

        [Fact]
        public async Task GenerateRecoveryCodes_TwoFactorDisabled_ReturnsBadRequest()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user-id",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(u => u.GetUserAsync(_user))
                .ReturnsAsync(user);

            _mockUserManager.Setup(u => u.GetTwoFactorEnabledAsync(user))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.GenerateRecoveryCodes();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("disabled", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task GenerateRecoveryCodes_NoUser_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserManager.Setup(u => u.GetUserAsync(_user))
                .ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _controller.GenerateRecoveryCodes();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not found.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task ResetAuthenticator_ValidUser_ReturnsSuccess()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user-id",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(u => u.GetUserAsync(_user))
                .ReturnsAsync(user);

            _mockUserManager.Setup(u => u.SetTwoFactorEnabledAsync(user, false))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(u => u.ResetAuthenticatorKeyAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.ResetAuthenticator();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic response = okResult.Value;

            // Check the success message
            Assert.Contains("reset", response.Message.ToString());

            // Verify methods were called
            _mockUserManager.Verify(u => u.SetTwoFactorEnabledAsync(user, false), Times.Once);
            _mockUserManager.Verify(u => u.ResetAuthenticatorKeyAsync(user), Times.Once);
        }

        [Fact]
        public async Task ResetAuthenticator_NoUser_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserManager.Setup(u => u.GetUserAsync(_user))
                .ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _controller.ResetAuthenticator();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not found.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task GetAuthenticatorKey_NoUser_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserManager.Setup(u => u.GetUserAsync(_user))
                .ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _controller.GetAuthenticatorKey();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not found.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task EnableTwoFactorAuthentication_NoUser_ReturnsUnauthorized()
        {
            // Arrange
            var model = new VerifyModel { Code = "123456" };

            _mockUserManager.Setup(u => u.GetUserAsync(_user))
                .ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _controller.EnableTwoFactorAuthentication(model);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not found.", unauthorizedResult.Value);
        }
    }
}
