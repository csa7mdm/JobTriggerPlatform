//using JobTriggerPlatform.Application.Abstractions;
//using JobTriggerPlatform.Infrastructure.Authorization;
//using JobTriggerPlatform.WebApi.Controllers;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging;
//using Moq;
//using System.Security.Claims;
//using Xunit;

//namespace JobTriggerPlatform.Tests.WebApi.Controllers
//{
//    public class JobsControllerTests
//    {
//        private readonly Mock<IAuthorizationService> _mockAuthService;
//        private readonly Mock<ILogger<JobsController>> _mockLogger;
//        private readonly List<Mock<IJobTriggerPlugin>> _mockPlugins;
//        private readonly JobsController _controller;

//        public JobsControllerTests()
//        {
//            _mockAuthService = new Mock<IAuthorizationService>();
//            _mockLogger = new Mock<ILogger<JobsController>>();
//            _mockPlugins = new List<Mock<IJobTriggerPlugin>>();

//            // Set up mock plugins
//            SetupMockPlugins();

//            // Create controller with mocked dependencies
//            _controller = new JobsController(
//                _mockPlugins.Select(p => p.Object),
//                _mockAuthService.Object,
//                _mockLogger.Object
//            );

//            // Set up controller context with user
//            SetupControllerContext();
//        }

//        private void SetupMockPlugins()
//        {
//            // Plugin 1 - Admin only
//            var plugin1 = new Mock<IJobTriggerPlugin>();
//            plugin1.Setup(p => p.JobName).Returns("AdminJob");
//            plugin1.Setup(p => p.RequiredRoles).Returns(new[] { "Admin" });
//            plugin1.Setup(p => p.Parameters).Returns(new[]
//            {
//                new PluginParameter { Name = "env", DisplayName = "Environment", IsRequired = true, Type = ParameterType.String },
//                new PluginParameter { Name = "version", DisplayName = "Version", IsRequired = false, Type = ParameterType.String }
//            });

//            // Plugin 2 - Operator role
//            var plugin2 = new Mock<IJobTriggerPlugin>();
//            plugin2.Setup(p => p.JobName).Returns("OperatorJob");
//            plugin2.Setup(p => p.RequiredRoles).Returns(new[] { "Operator" });
//            plugin2.Setup(p => p.Parameters).Returns(new[]
//            {
//                new PluginParameter { Name = "server", DisplayName = "Server", IsRequired = true, Type = ParameterType.String }
//            });

//            // Plugin 3 - Multiple roles
//            var plugin3 = new Mock<IJobTriggerPlugin>();
//            plugin3.Setup(p => p.JobName).Returns("CommonJob");
//            plugin3.Setup(p => p.RequiredRoles).Returns(new[] { "Admin", "Operator", "Viewer" });
//            plugin3.Setup(p => p.Parameters).Returns(new[]
//            {
//                new PluginParameter { Name = "param", DisplayName = "Parameter", IsRequired = false, Type = ParameterType.String }
//            });

//            _mockPlugins.Add(plugin1);
//            _mockPlugins.Add(plugin2);
//            _mockPlugins.Add(plugin3);
//        }

//        private void SetupControllerContext()
//        {
//            // Create ClaimsPrincipal with admin role
//            var claims = new List<Claim>
//            {
//                new Claim(ClaimTypes.Name, "testuser"),
//                new Claim(ClaimTypes.Role, "Admin"),
//                new Claim(ClaimTypes.Role, "Operator")
//            };
//            var identity = new ClaimsIdentity(claims, "Test");
//            var user = new ClaimsPrincipal(identity);

//            // Set up controller context
//            var httpContext = new DefaultHttpContext
//            {
//                User = user
//            };

//            _controller.ControllerContext = new ControllerContext
//            {
//                HttpContext = httpContext
//            };
//        }

//        [Fact]
//        public void GetJobs_ReturnsAccessiblePlugins()
//        {
//            // Act
//            var result = _controller.GetJobs();

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            var plugins = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
//            Assert.Equal(3, plugins.Count()); // All plugins are accessible because of Admin and Operator roles
//        }

//        [Fact]
//        public void GetJobs_WithViewerRoleOnly_ReturnsOnlyViewerAccessiblePlugins()
//        {
//            // Arrange
//            var claims = new List<Claim>
//            {
//                new Claim(ClaimTypes.Name, "testuser"),
//                new Claim(ClaimTypes.Role, "Viewer")
//            };
//            var identity = new ClaimsIdentity(claims, "Test");
//            var user = new ClaimsPrincipal(identity);

//            var httpContext = new DefaultHttpContext
//            {
//                User = user
//            };

//            _controller.ControllerContext = new ControllerContext
//            {
//                HttpContext = httpContext
//            };

//            // Act
//            var result = _controller.GetJobs();

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            var plugins = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
//            Assert.Single(plugins); // Only "CommonJob" should be accessible
//        }

//        [Fact]
//        public async Task GetJob_ExistingJobWithAccess_ReturnsJob()
//        {
//            // Arrange
//            string jobName = "AdminJob";
//            _mockAuthService.Setup(s => s.AuthorizeAsync(
//                It.IsAny<ClaimsPrincipal>(),
//                It.IsAny<object>(),
//                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
//                .Callback<ClaimsPrincipal, object, IEnumerable<IAuthorizationRequirement>>((user, resource, requirements) => {
//                    // Verify requirements contain JobAccessRequirement with correct JobName
//                    Assert.Contains(requirements, r => r is JobAccessRequirement jar && jar.JobName == jobName);
//                })
//                .ReturnsAsync(AuthorizationResult.Success());

//            // Act
//            var result = await _controller.GetJob(jobName);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            dynamic jobDetails = okResult.Value;
//            Assert.Equal(jobName, jobDetails.JobName);
//        }

//        [Fact]
//        public async Task GetJob_ExistingJobWithoutAccess_ReturnsForbid()
//        {
//            // Arrange
//            string jobName = "AdminJob";
//            _mockAuthService.Setup(s => s.AuthorizeAsync(
//                It.IsAny<ClaimsPrincipal>(),
//                It.IsAny<object>(),
//                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
//                .Callback<ClaimsPrincipal, object, IEnumerable<IAuthorizationRequirement>>((user, resource, requirements) => {
//                    // Verify requirements contain JobAccessRequirement with correct JobName
//                    Assert.Contains(requirements, r => r is JobAccessRequirement jar && jar.JobName == jobName);
//                })
//                .ReturnsAsync(AuthorizationResult.Failed());

//            // Act
//            var result = await _controller.GetJob(jobName);

//            // Assert
//            Assert.IsType<ForbidResult>(result);
//        }

//        [Fact]
//        public async Task GetJob_NonExistingJob_ReturnsNotFound()
//        {
//            // Arrange
//            string jobName = "NonExistingJob";
//            _mockAuthService.Setup(s => s.AuthorizeAsync(
//                It.IsAny<ClaimsPrincipal>(),
//                It.IsAny<object>(),
//                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
//                .Callback<ClaimsPrincipal, object, IEnumerable<IAuthorizationRequirement>>((user, resource, requirements) => {
//                    // Verify requirements contain JobAccessRequirement with correct JobName
//                    Assert.Contains(requirements, r => r is JobAccessRequirement jar && jar.JobName == jobName);
//                })
//                .ReturnsAsync(AuthorizationResult.Success());

//            // Act
//            var result = await _controller.GetJob(jobName);

//            // Assert
//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        [Fact]
//        public async Task TriggerJob_ValidParameters_ReturnsSuccess()
//        {
//            // Arrange
//            string jobName = "AdminJob";
//            _mockAuthService.Setup(s => s.AuthorizeAsync(
//                It.IsAny<ClaimsPrincipal>(),
//                It.IsAny<object>(),
//                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
//                .Callback<ClaimsPrincipal, object, IEnumerable<IAuthorizationRequirement>>((user, resource, requirements) => {
//                    // Verify requirements contain JobAccessRequirement with correct JobName
//                    Assert.Contains(requirements, r => r is JobAccessRequirement jar && jar.JobName == jobName);
//                })
//                .ReturnsAsync(AuthorizationResult.Success());

//            var parameters = new Dictionary<string, string>
//            {
//                { "env", "prod" }
//            };

//            _mockPlugins[0]
//                .Setup(p => p.TriggerAsync(It.IsAny<Dictionary<string, string>>()))
//                .ReturnsAsync(new PluginResult { IsSuccess = true, Details = "Job triggered successfully" });

//            // Act
//            var result = await _controller.TriggerJob(jobName, parameters);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            var jobResult = Assert.IsType<PluginResult>(okResult.Value);
//            Assert.True(jobResult.IsSuccess);
//            Assert.Equal("Job triggered successfully", jobResult.Details);
//        }

//        [Fact]
//        public async Task TriggerJob_MissingRequiredParameters_ReturnsBadRequest()
//        {
//            // Arrange
//            string jobName = "AdminJob";
//            _mockAuthService.Setup(s => s.AuthorizeAsync(
//                It.IsAny<ClaimsPrincipal>(),
//                It.IsAny<object>(),
//                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
//                .Callback<ClaimsPrincipal, object, IEnumerable<IAuthorizationRequirement>>((user, resource, requirements) => {
//                    // Verify requirements contain JobAccessRequirement with correct JobName
//                    Assert.Contains(requirements, r => r is JobAccessRequirement jar && jar.JobName == jobName);
//                })
//                .ReturnsAsync(AuthorizationResult.Success());

//            var parameters = new Dictionary<string, string>
//            {
//                { "version", "1.0.0" } // Missing required 'env' parameter
//            };

//            // Act
//            var result = await _controller.TriggerJob(jobName, parameters);

//            // Assert
//            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
//            Assert.Contains("Missing required parameters", badRequestResult.Value.ToString());
//        }

//        [Fact]
//        public async Task TriggerJob_WithoutAccess_ReturnsForbid()
//        {
//            // Arrange
//            string jobName = "AdminJob";
//            _mockAuthService.Setup(s => s.AuthorizeAsync(
//                It.IsAny<ClaimsPrincipal>(),
//                It.IsAny<object>(),
//                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
//                .Callback<ClaimsPrincipal, object, IEnumerable<IAuthorizationRequirement>>((user, resource, requirements) => {
//                    // Verify requirements contain JobAccessRequirement with correct JobName
//                    Assert.Contains(requirements, r => r is JobAccessRequirement jar && jar.JobName == jobName);
//                })
//                .ReturnsAsync(AuthorizationResult.Failed());

//            var parameters = new Dictionary<string, string>
//            {
//                { "env", "prod" }
//            };

//            // Act
//            var result = await _controller.TriggerJob(jobName, parameters);

//            // Assert
//            Assert.IsType<ForbidResult>(result);
//        }

//        [Fact]
//        public async Task TriggerJob_PluginThrowsException_ReturnsInternalServerError()
//        {
//            // Arrange
//            string jobName = "AdminJob";
//            _mockAuthService.Setup(s => s.AuthorizeAsync(
//                It.IsAny<ClaimsPrincipal>(),
//                It.IsAny<object>(),
//                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
//                .Callback<ClaimsPrincipal, object, IEnumerable<IAuthorizationRequirement>>((user, resource, requirements) => {
//                    // Verify requirements contain JobAccessRequirement with correct JobName
//                    Assert.Contains(requirements, r => r is JobAccessRequirement jar && jar.JobName == jobName);
//                })
//                .ReturnsAsync(AuthorizationResult.Success());

//            var parameters = new Dictionary<string, string>
//            {
//                { "env", "prod" }
//            };

//            _mockPlugins[0]
//                .Setup(p => p.TriggerAsync(It.IsAny<Dictionary<string, string>>()))
//                .ThrowsAsync(new Exception("Test exception"));

//            // Act
//            var result = await _controller.TriggerJob(jobName, parameters);

//            // Assert
//            var statusCodeResult = Assert.IsType<ObjectResult>(result);
//            Assert.Equal(500, statusCodeResult.StatusCode);
//        }

//        [Fact]
//        public async Task TriggerJob_PluginReturnsFailure_ReturnsBadRequest()
//        {
//            // Arrange
//            string jobName = "AdminJob";
//            _mockAuthService.Setup(s => s.AuthorizeAsync(
//                It.IsAny<ClaimsPrincipal>(),
//                It.IsAny<object>(),
//                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
//                .Callback<ClaimsPrincipal, object, IEnumerable<IAuthorizationRequirement>>((user, resource, requirements) => {
//                    // Verify requirements contain JobAccessRequirement with correct JobName
//                    Assert.Contains(requirements, r => r is JobAccessRequirement jar && jar.JobName == jobName);
//                })
//                .ReturnsAsync(AuthorizationResult.Success());

//            var parameters = new Dictionary<string, string>
//            {
//                { "env", "prod" }
//            };

//            _mockPlugins[0]
//                .Setup(p => p.TriggerAsync(It.IsAny<Dictionary<string, string>>()))
//                .ReturnsAsync(new PluginResult { IsSuccess = false, ErrorMessage = "Failed to trigger job" });

//            // Act
//            var result = await _controller.TriggerJob(jobName, parameters);

//            // Assert
//            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
//            var jobResult = Assert.IsType<PluginResult>(badRequestResult.Value);
//            Assert.False(jobResult.IsSuccess);
//            Assert.Equal("Failed to trigger job", jobResult.ErrorMessage);
//        }
//    }
//}
