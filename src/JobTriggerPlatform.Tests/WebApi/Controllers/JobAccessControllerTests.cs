using JobTriggerPlatform.Application.Abstractions;
using JobTriggerPlatform.Domain.Identity;
using JobTriggerPlatform.WebApi.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace JobTriggerPlatform.Tests.WebApi.Controllers
{
    public class JobAccessControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<ILogger<JobAccessController>> _mockLogger;
        private readonly List<Mock<IJobTriggerPlugin>> _mockPlugins;
        private readonly JobAccessController _controller;

        public JobAccessControllerTests()
        {
            // Setup UserManager mock
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null, null, null, null, null, null, null, null);

            // Setup Logger mock
            _mockLogger = new Mock<ILogger<JobAccessController>>();

            // Setup plugins
            _mockPlugins = new List<Mock<IJobTriggerPlugin>>();
            SetupMockPlugins();

            // Create controller
            _controller = new JobAccessController(
                _mockUserManager.Object,
                _mockPlugins.Select(p => p.Object),
                _mockLogger.Object);
        }

        private void SetupMockPlugins()
        {
            // Plugin 1 - Admin only
            var plugin1 = new Mock<IJobTriggerPlugin>();
            plugin1.Setup(p => p.JobName).Returns("AdminJob");
            plugin1.Setup(p => p.RequiredRoles).Returns(new[] { "Admin" });

            // Plugin 2 - Operator role
            var plugin2 = new Mock<IJobTriggerPlugin>();
            plugin2.Setup(p => p.JobName).Returns("OperatorJob");
            plugin2.Setup(p => p.RequiredRoles).Returns(new[] { "Operator" });

            // Plugin 3 - Multiple roles
            var plugin3 = new Mock<IJobTriggerPlugin>();
            plugin3.Setup(p => p.JobName).Returns("CommonJob");
            plugin3.Setup(p => p.RequiredRoles).Returns(new[] { "Admin", "Operator", "Viewer" });

            _mockPlugins.Add(plugin1);
            _mockPlugins.Add(plugin2);
            _mockPlugins.Add(plugin3);
        }

        [Fact]
        public async Task GetUsersWithJobAccess_ReturnsAllUsersWithJobAccess()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", Email = "admin@example.com", FullName = "Admin User" },
                new ApplicationUser { Id = "2", Email = "operator@example.com", FullName = "Operator User" }
            }.AsQueryable();

            _mockUserManager.Setup(u => u.Users).Returns(users);

            // User 1 has access to AdminJob and CommonJob
            _mockUserManager.Setup(u => u.GetClaimsAsync(It.Is<ApplicationUser>(user => user.Id == "1")))
                .ReturnsAsync(new List<Claim>
                {
                    new Claim("JobAccess", "AdminJob"),
                    new Claim("JobAccess", "CommonJob")
                });

            // User 2 has access to OperatorJob
            _mockUserManager.Setup(u => u.GetClaimsAsync(It.Is<ApplicationUser>(user => user.Id == "2")))
                .ReturnsAsync(new List<Claim>
                {
                    new Claim("JobAccess", "OperatorJob")
                });

            // Act
            var result = await _controller.GetUsersWithJobAccess();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var usersWithJobAccess = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Equal(2, usersWithJobAccess.Count());
        }

        [Fact]
        public async Task GetUserJobAccess_ValidUser_ReturnsJobAccess()
        {
            // Arrange
            var userId = "user-id";
            var user = new ApplicationUser
            {
                Id = userId,
                Email = "user@example.com",
                FullName = "Test User"
            };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync(user);

            // User has direct access to AdminJob and CommonJob
            _mockUserManager.Setup(u => u.GetClaimsAsync(user))
                .ReturnsAsync(new List<Claim>
                {
                    new Claim("JobAccess", "AdminJob"),
                    new Claim("JobAccess", "CommonJob")
                });

            // User has Admin role
            _mockUserManager.Setup(u => u.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Admin" });

            // Act
            var result = await _controller.GetUserJobAccess(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic userJobAccess = okResult.Value;

            // Check if the user has the expected direct job access
            var directJobAccess = (IEnumerable<string>)userJobAccess.DirectJobAccess;
            Assert.Equal(2, directJobAccess.Count());
            Assert.Contains("AdminJob", directJobAccess);
            Assert.Contains("CommonJob", directJobAccess);

            // Check if the user has the expected role-based job access
            var roleBasedJobAccess = (IEnumerable<string>)userJobAccess.RoleBasedJobAccess;
            Assert.Equal(2, roleBasedJobAccess.Count()); // AdminJob and CommonJob are accessible through the Admin role
            Assert.Contains("AdminJob", roleBasedJobAccess);
            Assert.Contains("CommonJob", roleBasedJobAccess);
        }

        [Fact]
        public async Task GetUserJobAccess_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            var userId = "non-existing-id";

            _mockUserManager.Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _controller.GetUserJobAccess(userId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateUserJobAccess_ValidModel_ReturnsUpdatedJobAccess()
        {
            // Arrange
            var userId = "user-id";
            var model = new JobAccessUpdateModel
            {
                JobAccess = new List<string> { "AdminJob", "OperatorJob" }
            };

            var user = new ApplicationUser
            {
                Id = userId,
                Email = "user@example.com",
                FullName = "Test User"
            };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync(user);

            // User currently has access to CommonJob
            _mockUserManager.Setup(u => u.GetClaimsAsync(user))
                .ReturnsAsync(new List<Claim>
                {
                    new Claim("JobAccess", "CommonJob")
                });

            _mockUserManager.Setup(u => u.RemoveClaimAsync(user, It.IsAny<Claim>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(u => u.AddClaimAsync(user, It.IsAny<Claim>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.UpdateUserJobAccess(userId, model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic updatedUser = okResult.Value;

            // Check if the user has the expected job access
            var jobAccess = (IEnumerable<string>)updatedUser.JobAccess;
            Assert.Equal(2, jobAccess.Count());
            Assert.Contains("AdminJob", jobAccess);
            Assert.Contains("OperatorJob", jobAccess);

            // Verify that the old claim was removed and new claims were added
            _mockUserManager.Verify(u => u.RemoveClaimAsync(user, It.IsAny<Claim>()), Times.Once);
            _mockUserManager.Verify(u => u.AddClaimAsync(user, It.IsAny<Claim>()), Times.Exactly(2));
        }

        [Fact]
        public async Task UpdateUserJobAccess_InvalidJobNames_ReturnsBadRequest()
        {
            // Arrange
            var userId = "user-id";
            var model = new JobAccessUpdateModel
            {
                JobAccess = new List<string> { "AdminJob", "NonExistingJob" }
            };

            var user = new ApplicationUser
            {
                Id = userId,
                Email = "user@example.com",
                FullName = "Test User"
            };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.UpdateUserJobAccess(userId, model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid job names", badRequestResult.Value.ToString());
            Assert.Contains("NonExistingJob", badRequestResult.Value.ToString());
        }

        [Fact]
        public void GetAvailableJobs_ReturnsAllJobs()
        {
            // Act
            var result = _controller.GetAvailableJobs();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var jobs = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Equal(3, jobs.Count());
        }

        [Fact]
        public async Task AddJobAccess_ValidUserAndJob_ReturnsSuccess()
        {
            // Arrange
            var userId = "user-id";
            var jobName = "AdminJob";

            var user = new ApplicationUser
            {
                Id = userId,
                Email = "user@example.com",
                FullName = "Test User"
            };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync(user);

            // User currently has no job access claims
            _mockUserManager.Setup(u => u.GetClaimsAsync(user))
                .ReturnsAsync(new List<Claim>());

            _mockUserManager.Setup(u => u.AddClaimAsync(user, It.IsAny<Claim>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.AddJobAccess(userId, jobName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic response = okResult.Value;

            Assert.Contains("added successfully", response.Message.ToString());
            Assert.Equal(jobName, response.JobName);
            Assert.Equal(userId, response.UserId);

            // Verify that the claim was added
            _mockUserManager.Verify(
                u => u.AddClaimAsync(
                    user,
                    It.Is<Claim>(c => c.Type == "JobAccess" && c.Value == jobName)),
                Times.Once);
        }

        [Fact]
        public async Task AddJobAccess_UserAlreadyHasAccess_ReturnsBadRequest()
        {
            // Arrange
            var userId = "user-id";
            var jobName = "AdminJob";

            var user = new ApplicationUser
            {
                Id = userId,
                Email = "user@example.com",
                FullName = "Test User"
            };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync(user);

            // User already has access to AdminJob
            _mockUserManager.Setup(u => u.GetClaimsAsync(user))
                .ReturnsAsync(new List<Claim>
                {
                    new Claim("JobAccess", "AdminJob")
                });

            // Act
            var result = await _controller.AddJobAccess(userId, jobName);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("already has access", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task RemoveJobAccess_ValidUserAndJob_ReturnsSuccess()
        {
            // Arrange
            var userId = "user-id";
            var jobName = "AdminJob";

            var user = new ApplicationUser
            {
                Id = userId,
                Email = "user@example.com",
                FullName = "Test User"
            };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync(user);

            // User has access to AdminJob
            var jobAccessClaim = new Claim("JobAccess", "AdminJob");
            _mockUserManager.Setup(u => u.GetClaimsAsync(user))
                .ReturnsAsync(new List<Claim> { jobAccessClaim });

            _mockUserManager.Setup(u => u.RemoveClaimAsync(user, It.IsAny<Claim>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.RemoveJobAccess(userId, jobName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic response = okResult.Value;

            Assert.Contains("removed successfully", response.Message.ToString());
            Assert.Equal(jobName, response.JobName);
            Assert.Equal(userId, response.UserId);

            // Verify that the claim was removed
            _mockUserManager.Verify(
                u => u.RemoveClaimAsync(
                    user,
                    It.Is<Claim>(c => c.Type == "JobAccess" && c.Value == jobName)),
                Times.Once);
        }

        [Fact]
        public async Task RemoveJobAccess_UserDoesNotHaveAccess_ReturnsBadRequest()
        {
            // Arrange
            var userId = "user-id";
            var jobName = "AdminJob";

            var user = new ApplicationUser
            {
                Id = userId,
                Email = "user@example.com",
                FullName = "Test User"
            };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync(user);

            // User does not have access to AdminJob
            _mockUserManager.Setup(u => u.GetClaimsAsync(user))
                .ReturnsAsync(new List<Claim>());

            // Act
            var result = await _controller.RemoveJobAccess(userId, jobName);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("does not have direct access", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task RemoveJobAccess_NonExistingJob_ReturnsNotFound()
        {
            // Arrange
            var userId = "user-id";
            var jobName = "NonExistingJob";

            var user = new ApplicationUser
            {
                Id = userId,
                Email = "user@example.com",
                FullName = "Test User"
            };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.RemoveJobAccess(userId, jobName);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task AddJobAccess_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            var userId = "non-existing-id";
            var jobName = "AdminJob";

            _mockUserManager.Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _controller.AddJobAccess(userId, jobName);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task AddJobAccess_NonExistingJob_ReturnsNotFound()
        {
            // Arrange
            var userId = "user-id";
            var jobName = "NonExistingJob";

            var user = new ApplicationUser
            {
                Id = userId,
                Email = "user@example.com",
                FullName = "Test User"
            };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.AddJobAccess(userId, jobName);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task RemoveJobAccess_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            var userId = "non-existing-id";
            var jobName = "AdminJob";

            _mockUserManager.Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _controller.RemoveJobAccess(userId, jobName);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateUserJobAccess_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            var userId = "non-existing-id";
            var model = new JobAccessUpdateModel
            {
                JobAccess = new List<string> { "AdminJob" }
            };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _controller.UpdateUserJobAccess(userId, model);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateUserJobAccess_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var userId = "user-id";
            var model = new JobAccessUpdateModel(); // Empty model

            // Add ModelState error
            _controller.ModelState.AddModelError("JobAccess", "The JobAccess field is required.");

            // Act
            var result = await _controller.UpdateUserJobAccess(userId, model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }
    }
}
