using JobTriggerPlatform.Domain.Identity;
using JobTriggerPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace JobTriggerPlatform.Tests.Infrastructure
{
    public class RoleSeederTests
    {
        [Fact]
        public async Task SeedRolesAsync_CreatesRolesCorrectly()
        {
            // Arrange
            var mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
                Mock.Of<IRoleStore<ApplicationRole>>(),
                null, null, null, null);

            mockRoleManager.Setup(m => m.RoleExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            mockRoleManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationRole>()))
                .ReturnsAsync(IdentityResult.Success);

            var mockLogger = new Mock<ILogger<RoleSeeder>>();

            var serviceProvider = new Mock<IServiceProvider>();
            var serviceScope = new Mock<IServiceScope>();
            var serviceScopeFactory = new Mock<IServiceScopeFactory>();

            serviceProvider.Setup(s => s.GetService(typeof(RoleManager<ApplicationRole>)))
                .Returns(mockRoleManager.Object);
            serviceProvider.Setup(s => s.GetService(typeof(ILogger<RoleSeeder>)))
                .Returns(mockLogger.Object);

            serviceScope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);
            serviceScopeFactory.Setup(x => x.CreateScope()).Returns(serviceScope.Object);
            serviceProvider.Setup(s => s.GetService(typeof(IServiceScopeFactory)))
                .Returns(serviceScopeFactory.Object);

            // Act
            await RoleSeeder.SeedRolesAsync(serviceProvider.Object);

            // Assert
            mockRoleManager.Verify(m => m.RoleExistsAsync(RoleSeeder.Roles.Admin), Times.Once);
            mockRoleManager.Verify(m => m.RoleExistsAsync(RoleSeeder.Roles.Operator), Times.Once);
            mockRoleManager.Verify(m => m.RoleExistsAsync(RoleSeeder.Roles.Viewer), Times.Once);
        }

        [Fact]
        public async Task SeedRolesAsync_DoesNotCreateRolesIfTheyExist()
        {
            // Arrange
            var mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
                Mock.Of<IRoleStore<ApplicationRole>>(),
                null, null, null, null);

            mockRoleManager.Setup(m => m.RoleExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            var mockLogger = new Mock<ILogger<RoleSeeder>>();

            var serviceProvider = new Mock<IServiceProvider>();
            var serviceScope = new Mock<IServiceScope>();
            var serviceScopeFactory = new Mock<IServiceScopeFactory>();

            serviceProvider.Setup(s => s.GetService(typeof(RoleManager<ApplicationRole>)))
                .Returns(mockRoleManager.Object);
            serviceProvider.Setup(s => s.GetService(typeof(ILogger<RoleSeeder>)))
                .Returns(mockLogger.Object);

            serviceScope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);
            serviceScopeFactory.Setup(x => x.CreateScope()).Returns(serviceScope.Object);
            serviceProvider.Setup(s => s.GetService(typeof(IServiceScopeFactory)))
                .Returns(serviceScopeFactory.Object);

            // Act
            await RoleSeeder.SeedRolesAsync(serviceProvider.Object);

            // Assert
            mockRoleManager.Verify(m => m.RoleExistsAsync(It.IsAny<string>()), Times.Exactly(3));
            mockRoleManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationRole>()), Times.Never);
        }
    }
}
