using JobTriggerPlatform.Domain.Identity;
using System;
using Xunit;

namespace JobTriggerPlatform.Tests.Domain
{
    public class ApplicationRoleTests
    {
        [Fact]
        public void Constructor_WithNoParameters_SetsPropertiesCorrectly()
        {
            // Act
            var role = new ApplicationRole();

            // Assert
            Assert.NotNull(role);
            Assert.Null(role.Description);
            Assert.True(Math.Abs((DateTime.UtcNow - role.CreatedAt).TotalSeconds) < 5);
        }

        [Fact]
        public void Constructor_WithRoleName_SetsPropertiesCorrectly()
        {
            // Arrange
            string roleName = "TestRole";

            // Act
            var role = new ApplicationRole(roleName);

            // Assert
            Assert.NotNull(role);
            Assert.Equal(roleName, role.Name);
            Assert.Null(role.Description);
            Assert.True(Math.Abs((DateTime.UtcNow - role.CreatedAt).TotalSeconds) < 5);
        }

        [Fact]
        public void Constructor_WithRoleNameAndDescription_SetsPropertiesCorrectly()
        {
            // Arrange
            string roleName = "TestRole";
            string description = "Test role description";

            // Act
            var role = new ApplicationRole(roleName, description);

            // Assert
            Assert.NotNull(role);
            Assert.Equal(roleName, role.Name);
            Assert.Equal(description, role.Description);
            Assert.True(Math.Abs((DateTime.UtcNow - role.CreatedAt).TotalSeconds) < 5);
        }
    }
}
