using JobTriggerPlatform.Domain.Identity;
using System;
using Xunit;

namespace JobTriggerPlatform.Tests.Domain
{
    public class ApplicationUserTests
    {
        [Fact]
        public void Constructor_SetsDefaultPropertiesCorrectly()
        {
            // Act
            var user = new ApplicationUser();

            // Assert
            Assert.NotNull(user);
            Assert.Null(user.FullName);
            Assert.True(Math.Abs((DateTime.UtcNow - user.CreatedAt).TotalSeconds) < 5);
            Assert.False(user.HasCompletedSetup);
            Assert.Null(user.LastLogin);
        }

        [Fact]
        public void SetProperties_UpdatesValuesCorrectly()
        {
            // Arrange
            var user = new ApplicationUser();
            string fullName = "Test User";
            DateTime lastLogin = DateTime.UtcNow.AddDays(-1);

            // Act
            user.FullName = fullName;
            user.HasCompletedSetup = true;
            user.LastLogin = lastLogin;

            // Assert
            Assert.Equal(fullName, user.FullName);
            Assert.True(user.HasCompletedSetup);
            Assert.Equal(lastLogin, user.LastLogin);
        }
    }
}
