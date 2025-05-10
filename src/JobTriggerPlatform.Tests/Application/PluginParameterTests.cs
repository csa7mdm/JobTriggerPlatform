using JobTriggerPlatform.Application.Abstractions;
using Xunit;

namespace JobTriggerPlatform.Tests.Application
{
    public class PluginParameterTests
    {
        [Fact]
        public void PluginParameter_PropertiesSetCorrectly()
        {
            // Arrange
            var name = "testParam";
            var displayName = "Test Parameter";
            var description = "This is a test parameter";
            var defaultValue = "default";
            var possibleValues = new[] { "value1", "value2", "value3" };

            // Act
            var parameter = new PluginParameter
            {
                Name = name,
                DisplayName = displayName,
                IsRequired = true,
                Description = description,
                DefaultValue = defaultValue,
                Type = ParameterType.Select,
                PossibleValues = possibleValues
            };

            // Assert
            Assert.Equal(name, parameter.Name);
            Assert.Equal(displayName, parameter.DisplayName);
            Assert.True(parameter.IsRequired);
            Assert.Equal(description, parameter.Description);
            Assert.Equal(defaultValue, parameter.DefaultValue);
            Assert.Equal(ParameterType.Select, parameter.Type);
            Assert.Equal(possibleValues, parameter.PossibleValues);
        }

        [Fact]
        public void PluginParameter_DefaultsForIsRequired()
        {
            // Act
            var parameter = new PluginParameter
            {
                Name = "param",
                DisplayName = "Parameter"
            };

            // Assert
            Assert.True(parameter.IsRequired); // Default is true for PluginParameter
        }

        [Fact]
        public void PluginParameter_DefaultsForType()
        {
            // Act
            var parameter = new PluginParameter
            {
                Name = "param",
                DisplayName = "Parameter"
            };

            // Assert
            Assert.Equal(ParameterType.String, parameter.Type); // Default is String
        }
    }

    public class PluginResultTests
    {
        [Fact]
        public void PluginResult_Success_Factory_CreatesCorrectResult()
        {
            // Arrange
            var data = new { Id = 1, Name = "Test" };
            var details = "Operation successful";
            var logs = new[] { "Started", "Completed" };

            // Act
            var result = PluginResult.Success(data, details, logs);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(data, result.Data);
            Assert.Equal(details, result.Details);
            Assert.Equal(logs, result.Logs);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void PluginResult_Failure_Factory_CreatesCorrectResult()
        {
            // Arrange
            var errorMessage = "Operation failed";
            var details = "An error occurred during processing";
            var logs = new[] { "Started", "Error", "Failed" };

            // Act
            var result = PluginResult.Failure(errorMessage, details, logs);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(errorMessage, result.ErrorMessage);
            Assert.Equal(details, result.Details);
            Assert.Equal(logs, result.Logs);
            Assert.Null(result.Data);
        }

        [Fact]
        public void PluginResult_PropertiesMutation()
        {
            // Arrange
            var result = new PluginResult
            {
                IsSuccess = true,
                Data = "Initial Data",
                Details = "Initial Details",
                Logs = new[] { "Initial Log" }
            };

            // Act
            result.IsSuccess = false;
            result.ErrorMessage = "Error occurred";
            result.Data = "Updated Data";
            result.Details = "Updated Details";
            result.Logs = new[] { "Updated Log" };

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Error occurred", result.ErrorMessage);
            Assert.Equal("Updated Data", result.Data);
            Assert.Equal("Updated Details", result.Details);
            Assert.Equal(new[] { "Updated Log" }, result.Logs);
        }
    }
}
