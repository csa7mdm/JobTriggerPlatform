using JobTriggerPlatform.Application.Abstractions;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace JobTriggerPlatform.Tests.Application
{
    public class IJobTriggerPluginTests
    {
        [Fact]
        public async Task IJobTriggerPlugin_MeetsExpectedInterface()
        {
            // Arrange
            var mockPlugin = new Mock<IJobTriggerPlugin>();
            mockPlugin.Setup(p => p.JobName).Returns("TestJob");
            mockPlugin.Setup(p => p.RequiredRoles).Returns(new[] { "Admin", "Operator" });
            
            var parameters = new List<PluginParameter>
            {
                new PluginParameter 
                { 
                    Name = "param1", 
                    DisplayName = "Parameter 1",
                    Description = "First parameter",
                    IsRequired = true,
                    Type = ParameterType.String
                },
                new PluginParameter 
                { 
                    Name = "param2", 
                    DisplayName = "Parameter 2",
                    Description = "Second parameter",
                    IsRequired = false,
                    Type = ParameterType.Select,
                    PossibleValues = new[] { "option1", "option2" }
                }
            };
            mockPlugin.Setup(p => p.Parameters).Returns(parameters);

            var jobParameters = new Dictionary<string, string>
            {
                { "param1", "value1" },
                { "param2", "option1" }
            };

            var logs = new List<string> { "Job started", "Job completed" };
            var result = PluginResult.Success(data: null, details: "Job executed successfully", logs: logs);

            mockPlugin.Setup(p => p.TriggerAsync(It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(result);

            // Act
            var jobName = mockPlugin.Object.JobName;
            var requiredRoles = mockPlugin.Object.RequiredRoles;
            var pluginParameters = mockPlugin.Object.Parameters;
            var triggerResult = await mockPlugin.Object.TriggerAsync(jobParameters);

            // Assert
            Assert.Equal("TestJob", jobName);
            Assert.Equal(new[] { "Admin", "Operator" }, requiredRoles);
            Assert.Equal(parameters, pluginParameters);
            Assert.Equal(result, triggerResult);
            Assert.True(triggerResult.IsSuccess);
            Assert.Equal("Job executed successfully", triggerResult.Details);
            Assert.Equal(logs, triggerResult.Logs);
            
            // Verify the mock was called with the correct parameters
            mockPlugin.Verify(p => p.TriggerAsync(jobParameters), Times.Once);
        }

        [Fact]
        public async Task IJobTriggerPlugin_HandlesFailureResult()
        {
            // Arrange
            var mockPlugin = new Mock<IJobTriggerPlugin>();
            mockPlugin.Setup(p => p.JobName).Returns("TestJob");
            
            var jobParameters = new Dictionary<string, string>
            {
                { "param1", "invalidValue" }
            };

            var logs = new List<string> { "Job started", "Parameter validation failed", "Job failed" };
            var result = PluginResult.Failure(
                errorMessage: "Invalid parameter value", 
                details: "The parameter value is not valid for the operation",
                logs: logs);

            mockPlugin.Setup(p => p.TriggerAsync(It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(result);

            // Act
            var triggerResult = await mockPlugin.Object.TriggerAsync(jobParameters);

            // Assert
            Assert.False(triggerResult.IsSuccess);
            Assert.Equal("Invalid parameter value", triggerResult.ErrorMessage);
            Assert.Equal("The parameter value is not valid for the operation", triggerResult.Details);
            Assert.Equal(logs, triggerResult.Logs);
            
            // Verify the mock was called with the correct parameters
            mockPlugin.Verify(p => p.TriggerAsync(jobParameters), Times.Once);
        }
    }
}
