using JobTriggerPlatform.Domain.Identity;
using JobTriggerPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace JobTriggerPlatform.Tests.Infrastructure
{
    public class UserSeederTests
    {
        [Fact]
        public async Task SeedUsersAsync_CreatesUsersCorrectly()
        {
            // Arrange
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            var mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
                Mock.Of<IRoleStore<ApplicationRole>>(),
                null, null, null, null);

            mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser)null);

            mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
                
            mockRoleManager.Setup(m => m.RoleExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            var mockLogger = new Mock<ILogger<UserSeeder>>();

            var serviceProvider = new Mock<IServiceProvider>();
            var serviceScope = new Mock<IServiceScope>();
            var serviceScopeFactory = new Mock<IServiceScopeFactory>();

            serviceProvider.Setup(s => s.GetService(typeof(UserManager<ApplicationUser>)))
                .Returns(mockUserManager.Object);
            serviceProvider.Setup(s => s.GetService(typeof(RoleManager<ApplicationRole>)))
                .Returns(mockRoleManager.Object);
            serviceProvider.Setup(s => s.GetService(typeof(ILogger<UserSeeder>)))
                .Returns(mockLogger.Object);

            serviceScope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);
            serviceScopeFactory.Setup(x => x.CreateScope()).Returns(serviceScope.Object);
            serviceProvider.Setup(s => s.GetService(typeof(IServiceScopeFactory)))
                .Returns(serviceScopeFactory.Object);

            // Act
            await UserSeeder.SeedUsersAsync(serviceProvider.Object);

            // Assert
            mockUserManager.Verify(m => m.FindByEmailAsync(UserSeeder.Users.Admin), Times.Once);
            mockUserManager.Verify(m => m.FindByEmailAsync(UserSeeder.Users.Operator), Times.Once);
            mockUserManager.Verify(m => m.FindByEmailAsync(UserSeeder.Users.Viewer), Times.Once);
            
            mockUserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Exactly(3));
            mockUserManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Exactly(3));
        }

        [Fact]
        public async Task SeedUsersAsync_DoesNotCreateUsersIfTheyExist()
        {
            // Arrange
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            var mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
                Mock.Of<IRoleStore<ApplicationRole>>(),
                null, null, null, null);

            mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(new ApplicationUser());

            var mockLogger = new Mock<ILogger<UserSeeder>>();

            var serviceProvider = new Mock<IServiceProvider>();
            var serviceScope = new Mock<IServiceScope>();
            var serviceScopeFactory = new Mock<IServiceScopeFactory>();

            serviceProvider.Setup(s => s.GetService(typeof(UserManager<ApplicationUser>)))
                .Returns(mockUserManager.Object);
            serviceProvider.Setup(s => s.GetService(typeof(RoleManager<ApplicationRole>)))
                .Returns(mockRoleManager.Object);
            serviceProvider.Setup(s => s.GetService(typeof(ILogger<UserSeeder>)))
                .Returns(mockLogger.Object);

            serviceScope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);
            serviceScopeFactory.Setup(x => x.CreateScope()).Returns(serviceScope.Object);
            serviceProvider.Setup(s => s.GetService(typeof(IServiceScopeFactory)))
                .Returns(serviceScopeFactory.Object);

            // Act
            await UserSeeder.SeedUsersAsync(serviceProvider.Object);

            // Assert
            mockUserManager.Verify(m => m.FindByEmailAsync(It.IsAny<string>()), Times.Exactly(3));
            mockUserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
            mockUserManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }
    }
}
