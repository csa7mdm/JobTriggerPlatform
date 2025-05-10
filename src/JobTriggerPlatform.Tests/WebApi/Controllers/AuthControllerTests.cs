using JobTriggerPlatform.Application.Interfaces;
using JobTriggerPlatform.Domain.Identity;
using JobTriggerPlatform.WebApi.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace JobTriggerPlatform.Tests.WebApi.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            // Set up UserManager mock
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // Set up SignInManager mock
            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object,
                new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
                null, null, null, null);

            // Set up Configuration mock
            _mockConfiguration = new Mock<IConfiguration>();
            var configurationSection = new Mock<IConfigurationSection>();
            configurationSection.Setup(s => s.Value).Returns("test_secret_key_that_is_at_least_32_characters_long");
            _mockConfiguration.Setup(c => c["JWT:Secret"]).Returns("test_secret_key_that_is_at_least_32_characters_long");
            _mockConfiguration.Setup(c => c["JWT:Issuer"]).Returns("test_issuer");
            _mockConfiguration.Setup(c => c["JWT:Audience"]).Returns("test_audience");
            _mockConfiguration.Setup(c => c["JWT:ExpiryInMinutes"]).Returns("60");
            _mockConfiguration.Setup(c => c["ClientUrl"]).Returns("http://localhost:3000");

            // Set up other mocks
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<AuthController>>();

            // Create controller
            _controller = new AuthController(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _mockConfiguration.Object,
                _mockEmailService.Object,
                _mockLogger.Object);

            // Set up controller context
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task Register_ValidModel_ReturnsOk()
        {
            // Arrange
            var model = new RegisterModel
            {
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Password = "Password123!",
                Role = "Viewer"
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser)null!);

            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("confirmation_token");

            // Set up URL helper for confirmation link
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
                .Returns("http://localhost:5000/api/Auth/confirm-email?userId=1&token=confirmation_token");
            _controller.Url = urlHelper.Object;

            // Act
            var result = await _controller.Register(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("User registered successfully", okResult.Value.ToString());

            _mockEmailService.Verify(e => e.SendEmailAsync(
                It.Is<string>(s => s == model.Email),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Register_ExistingUser_ReturnsBadRequest()
        {
            // Arrange
            var model = new RegisterModel
            {
                Email = "existing@example.com",
                FirstName = "Existing",
                LastName = "User",
                Password = "Password123!"
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(new ApplicationUser { Email = model.Email });

            // Act
            var result = await _controller.Register(model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("User with this email already exists", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task ConfirmEmail_ValidToken_RedirectsToLogin()
        {
            // Arrange
            var userId = "user-id";
            var token = "valid-token";

            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync(new ApplicationUser { Id = userId, Email = "test@example.com" });

            _mockUserManager.Setup(m => m.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), token))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.ConfirmEmail(userId, token);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("http://localhost:3000/login?emailConfirmed=true", redirectResult.Url);
        }

        [Fact]
        public async Task ConfirmEmail_InvalidUser_ReturnsNotFound()
        {
            // Arrange
            var userId = "invalid-user-id";
            var token = "valid-token";

            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _controller.ConfirmEmail(userId, token);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            // Arrange
            var model = new LoginModel
            {
                Email = "test@example.com",
                Password = "Password123!",
                RememberMe = false
            };

            var user = new ApplicationUser
            {
                Id = "user-id",
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);

            _mockUserManager.Setup(m => m.IsEmailConfirmedAsync(user))
                .ReturnsAsync(true);

            _mockSignInManager.Setup(s => s.PasswordSignInAsync(user, model.Password, model.RememberMe, It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Viewer" });

            // Act
            var result = await _controller.Login(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            Assert.True(okResult.Value.GetType().GetProperty("Token") != null);
        }

        [Fact]
        public async Task Login_UnconfirmedEmail_ReturnsUnauthorized()
        {
            // Arrange
            var model = new LoginModel
            {
                Email = "unconfirmed@example.com",
                Password = "Password123!",
                RememberMe = false
            };

            var user = new ApplicationUser
            {
                Id = "user-id",
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = false
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);

            _mockUserManager.Setup(m => m.IsEmailConfirmedAsync(user))
                .ReturnsAsync(false);

            // Add this setup for PasswordSignInAsync which might be called before the email confirmation check
            _mockSignInManager.Setup(s => s.PasswordSignInAsync(user, model.Password, model.RememberMe, It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            dynamic resultValue = unauthorizedResult.Value;
            Assert.Contains("Email not confirmed", resultValue.Message.ToString());
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var model = new LoginModel
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            var user = new ApplicationUser
            {
                Id = "user-id",
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);

            _mockUserManager.Setup(m => m.IsEmailConfirmedAsync(user))
                .ReturnsAsync(true);

            _mockSignInManager.Setup(s => s.PasswordSignInAsync(user, model.Password, It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            _mockUserManager.Setup(m => m.CheckPasswordAsync(user, model.Password))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            dynamic resultValue = unauthorizedResult.Value;
            Assert.Contains("Invalid credentials", resultValue.Message.ToString());
        }

        [Fact]
        public async Task Login_AccountLockedOut_ReturnsUnauthorized()
        {
            // Arrange
            var model = new LoginModel
            {
                Email = "locked@example.com",
                Password = "Password123!"
            };

            var user = new ApplicationUser
            {
                Id = "user-id",
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);

            _mockUserManager.Setup(m => m.IsEmailConfirmedAsync(user))
                .ReturnsAsync(true);

            _mockSignInManager.Setup(s => s.PasswordSignInAsync(user, model.Password, It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Account locked out", unauthorizedResult.Value.ToString());
        }

        [Fact]
        public async Task Login_RequiresTwoFactor_ReturnsRequiresTwoFactor()
        {
            // Arrange
            var model = new LoginModel
            {
                Email = "2fa@example.com",
                Password = "Password123!"
            };

            var user = new ApplicationUser
            {
                Id = "user-id",
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);

            _mockUserManager.Setup(m => m.IsEmailConfirmedAsync(user))
                .ReturnsAsync(true);

            _mockSignInManager.Setup(s => s.PasswordSignInAsync(user, model.Password, It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Check if the result indicates that 2FA is required
            var resultType = okResult.Value.GetType();
            var requiresTwoFactorProp = resultType.GetProperty("RequiresTwoFactor");
            Assert.NotNull(requiresTwoFactorProp);
            Assert.True((bool)requiresTwoFactorProp.GetValue(okResult.Value)!);
        }

        [Fact]
        public async Task LoginWithTwoFactor_ValidCode_ReturnsToken()
        {
            // Arrange
            var model = new TwoFactorModel
            {
                Code = "123456",
                RememberMe = false,
                RememberClient = false
            };

            var user = new ApplicationUser
            {
                Id = "user-id",
                UserName = "2fa@example.com",
                Email = "2fa@example.com"
            };

            _mockSignInManager.Setup(s => s.GetTwoFactorAuthenticationUserAsync())
                .ReturnsAsync(user);

            _mockSignInManager.Setup(s => s.TwoFactorAuthenticatorSignInAsync(model.Code, model.RememberMe, model.RememberClient))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Viewer" });

            // Act
            var result = await _controller.LoginWithTwoFactor(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            Assert.True(okResult.Value.GetType().GetProperty("Token") != null);
        }

        [Fact]
        public async Task LoginWithTwoFactor_InvalidCode_ReturnsUnauthorized()
        {
            // Arrange
            var model = new TwoFactorModel
            {
                Code = "invalid",
                RememberMe = false,
                RememberClient = false
            };

            var user = new ApplicationUser
            {
                Id = "user-id",
                UserName = "2fa@example.com",
                Email = "2fa@example.com"
            };

            _mockSignInManager.Setup(s => s.GetTwoFactorAuthenticationUserAsync())
                .ReturnsAsync(user);

            _mockSignInManager.Setup(s => s.TwoFactorAuthenticatorSignInAsync(model.Code, model.RememberMe, model.RememberClient))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _controller.LoginWithTwoFactor(model);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Invalid authenticator code", unauthorizedResult.Value.ToString());
        }

        [Fact]
        public async Task LoginWithTwoFactor_NoUser_ReturnsUnauthorized()
        {
            // Arrange
            var model = new TwoFactorModel
            {
                Code = "123456",
                RememberMe = false,
                RememberClient = false
            };

            _mockSignInManager.Setup(s => s.GetTwoFactorAuthenticationUserAsync())
                .ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _controller.LoginWithTwoFactor(model);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Invalid two-factor authentication attempt", unauthorizedResult.Value.ToString());
        }

        [Fact]
        public async Task EnableTwoFactorAuthentication_ValidUser_ReturnsQrCode()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user-id",
                UserName = "test@example.com",
                Email = "test@example.com"
            };

            // Setup HttpContext with ClaimsPrincipal
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = principal
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            _mockUserManager.Setup(m => m.GetAuthenticatorKeyAsync(user))
                .ReturnsAsync("AUTHKEY123456");

            // Act
            var result = await _controller.EnableTwoFactorAuthentication();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Check if the result contains expected properties
            var resultType = okResult.Value.GetType();
            Assert.NotNull(resultType.GetProperty("AuthenticatorKey"));
            Assert.NotNull(resultType.GetProperty("QrCodeUri"));
        }

        [Fact]
        public async Task VerifyTwoFactorAuthentication_ValidCode_ReturnsRecoveryCodes()
        {
            // Arrange
            var model = new VerifyTwoFactorModel
            {
                Code = "123456"
            };

            var user = new ApplicationUser
            {
                Id = "user-id",
                UserName = "test@example.com",
                Email = "test@example.com"
            };

            // Setup HttpContext with ClaimsPrincipal
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = principal
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            _mockUserManager.Setup(m => m.VerifyTwoFactorTokenAsync(
                    user,
                    It.IsAny<string>(),
                    model.Code))
                .ReturnsAsync(true);

            _mockUserManager.Setup(m => m.SetTwoFactorEnabledAsync(user, true))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))
                .ReturnsAsync(new[] { "code1", "code2", "code3" });

            // Act
            var result = await _controller.VerifyTwoFactorAuthentication(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Check if the result contains recovery codes
            var resultType = okResult.Value.GetType();
            var recoveryCodesProp = resultType.GetProperty("RecoveryCodes");
            Assert.NotNull(recoveryCodesProp);

            var recoveryCodes = (IEnumerable<string>)recoveryCodesProp.GetValue(okResult.Value)!;
            Assert.Equal(3, recoveryCodes.Count());
        }

        [Fact]
        public async Task DisableTwoFactorAuthentication_ValidUser_ReturnsSuccess()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user-id",
                UserName = "test@example.com",
                Email = "test@example.com"
            };

            // Setup HttpContext with ClaimsPrincipal
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = principal
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            _mockUserManager.Setup(m => m.SetTwoFactorEnabledAsync(user, false))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.DisableTwoFactorAuthentication();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Two-factor authentication has been disabled", okResult.Value.ToString());
        }
    }
}
