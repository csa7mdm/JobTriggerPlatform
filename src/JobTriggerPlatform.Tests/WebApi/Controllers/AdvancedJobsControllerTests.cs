//using JobTriggerPlatform.Application.Abstractions;
//using JobTriggerPlatform.WebApi.Controllers;

//namespace JobTriggerPlatform.Tests.WebApi.Controllers
//{
//    public class AdvancedJobsControllerTests
//    {
//        private readonly Mock<ILogger<AdvancedJobsController>> _mockLogger;
//        private readonly List<Mock<IJobTriggerPlugin>> _mockPlugins;
//        private readonly AdvancedJobsController _controller;

//        public AdvancedJobsControllerTests()
//        {
//            // Setup Logger mock
//            _mockLogger = new Mock<ILogger<AdvancedJobsController>>();

//            // Setup plugins
//            _mockPlugins = new List<Mock<IJobTriggerPlugin>>();
//            SetupMockPlugins();

//            // Create controller
//            _controller = new AdvancedJobsController(
//                _mockPlugins.Select(p => p.Object),
//                _mockLogger.Object);
//        }

//        private void SetupMockPlugins()
//        {
//            // Sample Job
//            var samplePlugin = new Mock<IJobTriggerPlugin>();
//            samplePlugin.Setup(p => p.JobName).Returns("SampleJob");
//            samplePlugin.Setup(p => p.Parameters).Returns(new[]
//            {
//                new PluginParameter { Name = "env", DisplayName = "Environment", IsRequired = true, Type = ParameterType.String },
//                new PluginParameter { Name = "version", DisplayName = "Version", IsRequired = false, Type = ParameterType.String }
//            });

//            // Advanced Deployment Job
//            var advancedPlugin = new Mock<IJobTriggerPlugin>();
//            advancedPlugin.Setup(p => p.JobName).Returns("AdvancedDeployment");
//            advancedPlugin.Setup(p => p.Parameters).Returns(new[]
//            {
//                new PluginParameter { Name = "target", DisplayName = "Target", IsRequired = true, Type = ParameterType.String },
//                new PluginParameter { Name = "config", DisplayName = "Configuration", IsRequired = true, Type = ParameterType.String }
//            });

//            _mockPlugins.Add(samplePlugin);
//            _mockPlugins.Add(advancedPlugin);
//        }

//        [Fact]
//        public void GetSampleJob_JobExists_ReturnsJobDetails()
//        {
//            // Act
//            var result = _controller.GetSampleJob();

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            var jobDetails = okResult.Value as object;
//            Assert.NotNull(jobDetails);

//            // Use reflection to get the JobName property
//            var jobNameProperty = jobDetails.GetType().GetProperty("JobName");
//            Assert.NotNull(jobNameProperty);
//            var jobName = jobNameProperty.GetValue(jobDetails);
//            Assert.Equal("SampleJob", jobName);

//            // Use reflection to get the Parameters property
//            var parametersProperty = jobDetails.GetType().GetProperty("Parameters");
//            Assert.NotNull(parametersProperty);
//            var parameters = parametersProperty.GetValue(jobDetails) as IEnumerable<PluginParameter>;
//            Assert.NotNull(parameters);
//            Assert.Equal(2, parameters.Count());
//        }

//        [Fact]
//        public void GetSampleJob_JobDoesNotExist_ReturnsNotFound()
//        {
//            // Arrange
//            // Clear the plugins list and add a plugin with a different name
//            _mockPlugins.Clear();
//            var differentPlugin = new Mock<IJobTriggerPlugin>();
//            differentPlugin.Setup(p => p.JobName).Returns("DifferentJob");
//            _mockPlugins.Add(differentPlugin);

//            // Create a new controller with the updated plugins
//            var controller = new AdvancedJobsController(
//                _mockPlugins.Select(p => p.Object),
//                _mockLogger.Object);

//            // Act
//            var result = controller.GetSampleJob();

//            // Assert
//            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
//            Assert.Equal("Sample job not found.", notFoundResult.Value);
//        }

//        [Fact]
//        public async Task TriggerSampleJob_ValidParameters_ReturnsSuccess()
//        {
//            // Arrange
//            var parameters = new Dictionary<string, string>
//            {
//                { "env", "production" },
//                { "version", "1.0.0" }
//            };

//            _mockPlugins[0].Setup(p => p.TriggerAsync(It.IsAny<Dictionary<string, string>>()))
//                .ReturnsAsync(new PluginResult
//                {
//                    IsSuccess = true,
//                    Details = "Job triggered successfully"
//                });

//            // Act
//            var result = await _controller.TriggerSampleJob(parameters);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            var jobResult = Assert.IsType<PluginResult>(okResult.Value);
//            Assert.True(jobResult.IsSuccess);
//            Assert.Equal("Job triggered successfully", jobResult.Details);
//        }

//        [Fact]
//        public async Task TriggerSampleJob_JobFailure_ReturnsBadRequest()
//        {
//            // Arrange
//            var parameters = new Dictionary<string, string>
//            {
//                { "env", "invalid" }
//            };

//            _mockPlugins[0].Setup(p => p.TriggerAsync(It.IsAny<Dictionary<string, string>>()))
//                .ReturnsAsync(new PluginResult
//                {
//                    IsSuccess = false,
//                    ErrorMessage = "Invalid environment specified"
//                });

//            // Act
//            var result = await _controller.TriggerSampleJob(parameters);

//            // Assert
//            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
//            var jobResult = Assert.IsType<PluginResult>(badRequestResult.Value);
//            Assert.False(jobResult.IsSuccess);
//            Assert.Equal("Invalid environment specified", jobResult.ErrorMessage);
//        }

//        [Fact]
//        public async Task TriggerSampleJob_JobDoesNotExist_ReturnsNotFound()
//        {
//            // Arrange
//            _mockPlugins.Clear();
//            var differentPlugin = new Mock<IJobTriggerPlugin>();
//            differentPlugin.Setup(p => p.JobName).Returns("DifferentJob");
//            _mockPlugins.Add(differentPlugin);

//            var controller = new AdvancedJobsController(
//                _mockPlugins.Select(p => p.Object),
//                _mockLogger.Object);

//            var parameters = new Dictionary<string, string>
//            {
//                { "env", "production" }
//            };

//            // Act
//            var result = await controller.TriggerSampleJob(parameters);

//            // Assert
//            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
//            Assert.Equal("Sample job not found.", notFoundResult.Value);
//        }

//        [Fact]
//        public async Task TriggerSampleJob_ThrowsException_ReturnsInternalServerError()
//        {
//            // Arrange
//            var parameters = new Dictionary<string, string>
//            {
//                { "env", "production" }
//            };

//            _mockPlugins[0].Setup(p => p.TriggerAsync(It.IsAny<Dictionary<string, string>>()))
//                .ThrowsAsync(new Exception("Test exception"));

//            // Act
//            var result = await _controller.TriggerSampleJob(parameters);

//            // Assert
//            var statusCodeResult = Assert.IsType<ObjectResult>(result);
//            Assert.Equal(500, statusCodeResult.StatusCode);
//            Assert.Equal("An error occurred while triggering the job.", statusCodeResult.Value);
//        }

//        [Fact]
//        public void GetAdvancedDeployment_JobExists_ReturnsJobDetails()
//        {
//            // Act
//            var result = _controller.GetAdvancedDeployment();

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            var jobDetails = okResult.Value as object;
//            Assert.NotNull(jobDetails);

//            // Use reflection to get the JobName property
//            var jobNameProperty = jobDetails.GetType().GetProperty("JobName");
//            Assert.NotNull(jobNameProperty);
//            var jobName = jobNameProperty.GetValue(jobDetails);
//            Assert.Equal("AdvancedDeployment", jobName);

//            // Use reflection to get the Parameters property
//            var parametersProperty = jobDetails.GetType().GetProperty("Parameters");
//            Assert.NotNull(parametersProperty);
//            var parameters = parametersProperty.GetValue(jobDetails) as IEnumerable<PluginParameter>;
//            Assert.NotNull(parameters);
//            Assert.Equal(2, parameters.Count());
//        }

//        [Fact]
//        public void GetAdvancedDeployment_JobDoesNotExist_ReturnsNotFound()
//        {
//            // Arrange
//            _mockPlugins.Clear();
//            var differentPlugin = new Mock<IJobTriggerPlugin>();
//            differentPlugin.Setup(p => p.JobName).Returns("DifferentJob");
//            _mockPlugins.Add(differentPlugin);

//            var controller = new AdvancedJobsController(
//                _mockPlugins.Select(p => p.Object),
//                _mockLogger.Object);

//            // Act
//            var result = controller.GetAdvancedDeployment();

//            // Assert
//            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
//            Assert.Equal("Advanced deployment job not found.", notFoundResult.Value);
//        }

//        [Fact]
//        public async Task TriggerAdvancedDeployment_ValidParameters_ReturnsSuccess()
//        {
//            // Arrange
//            var parameters = new Dictionary<string, string>
//            {
//                { "target", "production" },
//                { "config", "release" }
//            };

//            _mockPlugins[1].Setup(p => p.TriggerAsync(It.IsAny<Dictionary<string, string>>()))
//                .ReturnsAsync(new PluginResult
//                {
//                    IsSuccess = true,
//                    Details = "Advanced deployment triggered successfully"
//                });

//            // Act
//            var result = await _controller.TriggerAdvancedDeployment(parameters);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            var jobResult = Assert.IsType<PluginResult>(okResult.Value);
//            Assert.True(jobResult.IsSuccess);
//            Assert.Equal("Advanced deployment triggered successfully", jobResult.Details);
//        }

//        [Fact]
//        public async Task TriggerAdvancedDeployment_JobFailure_ReturnsBadRequest()
//        {
//            // Arrange
//            var parameters = new Dictionary<string, string>
//            {
//                { "target", "invalid" }
//            };

//            _mockPlugins[1].Setup(p => p.TriggerAsync(It.IsAny<Dictionary<string, string>>()))
//                .ReturnsAsync(new PluginResult
//                {
//                    IsSuccess = false,
//                    ErrorMessage = "Invalid target specified"
//                });

//            // Act
//            var result = await _controller.TriggerAdvancedDeployment(parameters);

//            // Assert
//            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
//            var jobResult = Assert.IsType<PluginResult>(badRequestResult.Value);
//            Assert.False(jobResult.IsSuccess);
//            Assert.Equal("Invalid target specified", jobResult.ErrorMessage);
//        }

//        [Fact]
//        public async Task TriggerAdvancedDeployment_JobDoesNotExist_ReturnsNotFound()
//        {
//            // Arrange
//            _mockPlugins.Clear();
//            var differentPlugin = new Mock<IJobTriggerPlugin>();
//            differentPlugin.Setup(p => p.JobName).Returns("DifferentJob");
//            _mockPlugins.Add(differentPlugin);

//            var controller = new AdvancedJobsController(
//                _mockPlugins.Select(p => p.Object),
//                _mockLogger.Object);

//            var parameters = new Dictionary<string, string>
//            {
//                { "target", "production" }
//            };

//            // Act
//            var result = await controller.TriggerAdvancedDeployment(parameters);

//            // Assert
//            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
//            Assert.Equal("Advanced deployment job not found.", notFoundResult.Value);
//        }

//        [Fact]
//        public async Task TriggerAdvancedDeployment_ThrowsException_ReturnsInternalServerError()
//        {
//            // Arrange
//            var parameters = new Dictionary<string, string>
//            {
//                { "target", "production" }
//            };

//            _mockPlugins[1].Setup(p => p.TriggerAsync(It.IsAny<Dictionary<string, string>>()))
//                .ThrowsAsync(new Exception("Test exception"));

//            // Act
//            var result = await _controller.TriggerAdvancedDeployment(parameters);

//            // Assert
//            var statusCodeResult = Assert.IsType<ObjectResult>(result);
//            Assert.Equal(500, statusCodeResult.StatusCode);
//            Assert.Equal("An error occurred while triggering the job.", statusCodeResult.Value);
//        }
//    }
//}
