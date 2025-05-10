using JobTriggerPlatform.Application.Abstractions;
using JobTriggerPlatform.Domain.Identity;
using JobTriggerPlatform.WebApi.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace JobTriggerPlatform.Tests.WebApi.Controllers
{
    public class RolesControllerTests
    {
        private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<ILogger<RolesController>> _mockLogger;
        private readonly List<Mock<IJobTriggerPlugin>> _mockPlugins;
        private readonly RolesController _controller;

        public RolesControllerTests()
        {
            // Setup RoleManager mock
            var roleStore = new Mock<IRoleStore<ApplicationRole>>();
            _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
                roleStore.Object, null, null, null, null);

            // Setup UserManager mock
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null, null, null, null, null, null, null, null);

            // Setup Logger mock
            _mockLogger = new Mock<ILogger<RolesController>>();

            // Setup plugins
            _mockPlugins = new List<Mock<IJobTriggerPlugin>>();
            SetupMockPlugins();

            // Create controller
            _controller = new RolesController(
                _mockRoleManager.Object,
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
        public void GetRoles_ReturnsAllRoles()
        {
            // Arrange
            var roles = new List<ApplicationRole>
            {
                new ApplicationRole("Admin", "Administrator role") { Id = "1", CreatedAt = DateTime.UtcNow.AddDays(-10) },
                new ApplicationRole("Operator", "Operator role") { Id = "2", CreatedAt = DateTime.UtcNow.AddDays(-5) },
                new ApplicationRole("Viewer", "Viewer role") { Id = "3", CreatedAt = DateTime.UtcNow.AddDays(-2) }
            }.AsQueryable();

            _mockRoleManager.Setup(r => r.Roles).Returns(roles);

            // Act
            var result = _controller.GetRoles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedRoles = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Equal(3, returnedRoles.Count());
        }

        [Fact]
        public async Task CreateRole_ValidModel_ReturnsCreatedRole()
        {
            // Arrange
            var model = new RoleModel
            {
                Name = "TestRole",
                Description = "Test role description"
            };

            _mockRoleManager.Setup(r => r.RoleExistsAsync(model.Name))
                .ReturnsAsync(false);

            _mockRoleManager.Setup(r => r.CreateAsync(It.IsAny<ApplicationRole>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.CreateRole(model);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(RolesController.GetRoles), createdResult.ActionName);
            
            var createdRole = Assert.IsAssignableFrom<object>(createdResult.Value);
            var roleProperties = createdRole.GetType().GetProperties();
            
            Assert.Contains(roleProperties, p => p.Name == "Id");
            Assert.Contains(roleProperties, p => p.Name == "Name");
            Assert.Contains(roleProperties, p => p.Name == "Description");
            Assert.Contains(roleProperties, p => p.Name == "CreatedAt");
        }

        [Fact]
        public async Task CreateRole_ExistingRole_ReturnsBadRequest()
        {
            // Arrange
            var model = new RoleModel
            {
                Name = "ExistingRole",
                Description = "This role already exists"
            };

            _mockRoleManager.Setup(r => r.RoleExistsAsync(model.Name))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CreateRole(model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Role already exists.", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateRole_ValidModel_ReturnsUpdatedRole()
        {
            // Arrange
            var roleId = "test-role-id";
            var model = new RoleModel
            {
                Name = "UpdatedRole",
                Description = "Updated role description"
            };

            var existingRole = new ApplicationRole
            {
                Id = roleId,
                Name = "OriginalRole",
                Description = "Original description",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };

            _mockRoleManager.Setup(r => r.FindByIdAsync(roleId))
                .ReturnsAsync(existingRole);

            _mockRoleManager.Setup(r => r.UpdateAsync(It.IsAny<ApplicationRole>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.UpdateRole(roleId, model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedRole = Assert.IsAssignableFrom<object>(okResult.Value);
            var roleProperties = updatedRole.GetType().GetProperties();
            
            Assert.Contains(roleProperties, p => p.Name == "Id");
            Assert.Contains(roleProperties, p => p.Name == "Name");
            Assert.Contains(roleProperties, p => p.Name == "Description");
            Assert.Contains(roleProperties, p => p.Name == "CreatedAt");
            
            // Verify role was updated
            Assert.Equal(model.Name, existingRole.Name);
            Assert.Equal(model.Description, existingRole.Description);
        }

        [Fact]
        public async Task UpdateRole_NonExistingRole_ReturnsNotFound()
        {
            // Arrange
            var roleId = "non-existing-id";
            var model = new RoleModel
            {
                Name = "UpdatedRole",
                Description = "Updated role description"
            };

            _mockRoleManager.Setup(r => r.FindByIdAsync(roleId))
                .ReturnsAsync((ApplicationRole)null!);

            // Act
            var result = await _controller.UpdateRole(roleId, model);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Role not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteRole_NonExistingRole_ReturnsNotFound()
        {
            // Arrange
            var roleId = "non-existing-id";

            _mockRoleManager.Setup(r => r.FindByIdAsync(roleId))
                .ReturnsAsync((ApplicationRole)null!);

            // Act
            var result = await _controller.DeleteRole(roleId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Role not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteRole_PredefinedRole_ReturnsBadRequest()
        {
            // Arrange
            var roleId = "admin-role-id";
            var adminRole = new ApplicationRole("Admin", "Administrator role") { Id = roleId };

            _mockRoleManager.Setup(r => r.FindByIdAsync(roleId))
                .ReturnsAsync(adminRole);

            // Act
            var result = await _controller.DeleteRole(roleId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Cannot delete predefined roles.", badRequestResult.Value);
        }

        [Fact]
        public async Task DeleteRole_CustomRole_ReturnsNoContent()
        {
            // Arrange
            var roleId = "custom-role-id";
            var customRole = new ApplicationRole("CustomRole", "Custom role") { Id = roleId };

            _mockRoleManager.Setup(r => r.FindByIdAsync(roleId))
                .ReturnsAsync(customRole);

            _mockRoleManager.Setup(r => r.DeleteAsync(customRole))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.DeleteRole(roleId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetUsersWithRoles_ReturnsAllUsersWithRoles()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", Email = "admin@example.com", FullName = "Admin User" },
                new ApplicationUser { Id = "2", Email = "operator@example.com", FullName = "Operator User" },
                new ApplicationUser { Id = "3", Email = "viewer@example.com", FullName = "Viewer User" }
            }.AsQueryable();

            _mockUserManager.Setup(u => u.Users).Returns(users);

            _mockUserManager.Setup(u => u.GetRolesAsync(It.Is<ApplicationUser>(user => user.Id == "1")))
                .ReturnsAsync(new List<string> { "Admin" });

            _mockUserManager.Setup(u => u.GetRolesAsync(It.Is<ApplicationUser>(user => user.Id == "2")))
                .ReturnsAsync(new List<string> { "Operator" });

            _mockUserManager.Setup(u => u.GetRolesAsync(It.Is<ApplicationUser>(user => user.Id == "3")))
                .ReturnsAsync(new List<string> { "Viewer" });

            // Act
            var result = await _controller.GetUsersWithRoles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var userRoles = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Equal(3, userRoles.Count());
        }

        [Fact]
        public async Task UpdateUserRoles_ValidUser_ReturnsUpdatedUserRoles()
        {
            // Arrange
            var userId = "user-id";
            var model = new UserRolesModel
            {
                Roles = new List<string> { "Admin", "Operator" }
            };

            var user = new ApplicationUser
            {
                Id = userId,
                Email = "user@example.com",
                FullName = "Test User"
            };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync(user);

            _mockUserManager.Setup(u => u.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Viewer" });

            _mockUserManager.Setup(u => u.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(u => u.AddToRolesAsync(user, model.Roles))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.UpdateUserRoles(userId, model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedUser = Assert.IsAssignableFrom<object>(okResult.Value);

            // Verify the returned user has expected roles
            var userProperties = updatedUser.GetType().GetProperties();
            var rolesProp = userProperties.FirstOrDefault(p => p.Name == "Roles");
            Assert.NotNull(rolesProp);

            var roles = (IList<string>)rolesProp.GetValue(updatedUser)!;
            Assert.Equal(2, roles.Count);
            Assert.Contains("Admin", roles);
            Assert.Contains("Operator", roles);
        }

        [Fact]
        public async Task UpdateUserRoles_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            var userId = "non-existing-id";
            var model = new UserRolesModel
            {
                Roles = new List<string> { "Admin" }
            };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync((ApplicationUser)null!);

            // Act
            var result = await _controller.UpdateUserRoles(userId, model);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", notFoundResult.Value);
        }

        [Fact]
        public void GetPlugins_ReturnsAllPluginsWithRequiredRoles()
        {
            // Act
            var result = _controller.GetPlugins();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var plugins = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Equal(3, plugins.Count());

            // Verify each plugin has the expected properties
            foreach (var plugin in plugins)
            {
                var pluginType = plugin.GetType();
                Assert.NotNull(pluginType.GetProperty("JobName"));
                Assert.NotNull(pluginType.GetProperty("RequiredRoles"));
            }
        }
    }
}
