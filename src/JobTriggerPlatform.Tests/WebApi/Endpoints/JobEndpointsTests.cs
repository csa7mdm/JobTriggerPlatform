//using JobTriggerPlatform.Application.Abstractions;
//using JobTriggerPlatform.WebApi;
//using JobTriggerPlatform.WebApi.Models;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Routing;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using Moq;
//using System.Security.Claims;
//using Xunit;

//namespace JobTriggerPlatform.Tests.WebApi.Endpoints
//{
//    public class JobEndpointsTests
//    {
//        private readonly List<Mock<IJobTriggerPlugin>> _mockPlugins;
//        private readonly Mock<ILogger> _mockLogger; // Using non-generic ILogger
//        private readonly ClaimsPrincipal _user;

//        public JobEndpointsTests()
//        {
//            // Setup mock plugins
//            _mockPlugins = new List<Mock<IJobTriggerPlugin>>();
//            SetupMockPlugins();

//            // Setup generic logger
//            _mockLogger = new Mock<ILogger>();

//            // Setup user with claims
//            var claims = new List<Claim>
//            {
//                new Claim(ClaimTypes.NameIdentifier, "user-id"),
//                new Claim(ClaimTypes.Name, "test-user"),
//                new Claim(ClaimTypes.Role, "Admin"),
//                new Claim("JobAccess", "SampleJob")
//            };
//            _user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthentication"));
//        }

//        private void SetupMockPlugins()
//        {
//            // Sample Job
//            var sampleJob = new Mock<IJobTriggerPlugin>();
//            sampleJob.Setup(p => p.JobName).Returns("SampleJob");
//            sampleJob.Setup(p => p.RequiredRoles).Returns(new[] { "Admin" });

//            var sampleJobParams = new List<PluginParameter>
//            {
//                new PluginParameter
//                {
//                    Name = "environment",
//                    DisplayName = "Environment",
//                    Description = "Target environment",
//                    IsRequired = true,
//                    Type = ParameterType.Select,
//                    PossibleValues = new[] { "Development", "QA", "Production" }
//                },
//                new PluginParameter
//                {
//                    Name = "version",
//                    DisplayName = "Version",
//                    Description = "Version to deploy",
//                    IsRequired = true,
//                    Type = ParameterType.String
//                },
//                new PluginParameter
//                {
//                    Name = "skipTests",
//                    DisplayName = "Skip Tests",
//                    Description = "Skip test execution",
//                    IsRequired = false,
//                    Type = ParameterType.Boolean,
//                    DefaultValue = "false"
//                }
//            };
//            sampleJob.Setup(p => p.Parameters).Returns(sampleJobParams);

//            // Advanced Deployment Job
//            var advancedJob = new Mock<IJobTriggerPlugin>();
//            advancedJob.Setup(p => p.JobName).Returns("AdvancedDeployment");
//            advancedJob.Setup(p => p.RequiredRoles).Returns(new[] { "Admin", "Operator" });

//            var advancedJobParams = new List<PluginParameter>
//            {
//                new PluginParameter
//                {
//                    Name = "environment",
//                    DisplayName = "Environment",
//                    Description = "Target environment",
//                    IsRequired = true,
//                    Type = ParameterType.Select,
//                    PossibleValues = new[] { "Development", "QA", "Production" }
//                },
//                new PluginParameter
//                {
//                    Name = "version",
//                    DisplayName = "Version",
//                    Description = "Version to deploy",
//                    IsRequired = true,
//                    Type = ParameterType.String
//                },
//                new PluginParameter
//                {
//                    Name = "notifyUsers",
//                    DisplayName = "Notify Users",
//                    Description = "Send notification to users",
//                    IsRequired = false,
//                    Type = ParameterType.Boolean,
//                    DefaultValue = "false"
//                },
//                new PluginParameter
//                {
//                    Name = "deployDate",
//                    DisplayName = "Deploy Date",
//                    Description = "Schedule deployment for a future date",
//                    IsRequired = false,
//                    Type = ParameterType.Date
//                }
//            };
//            advancedJob.Setup(p => p.Parameters).Returns(advancedJobParams);

//            _mockPlugins.Add(sampleJob);
//            _mockPlugins.Add(advancedJob);
//        }

//        [Fact]
//        public void MapJobEndpoints_RegistersAllExpectedEndpoints()
//        {
//            // Arrange
//            var builder = WebApplication.CreateBuilder();

//            // Register FluentValidation
//            builder.Services.AddScoped<JobTriggerRequestValidator>();

//            // Configure necessary services
//            builder.Services.AddEndpointsApiExplorer();

//            // Register the logger
//            builder.Services.AddSingleton<ILogger>(_mockLogger.Object);

//            // Register plugin mocks
//            foreach (var pluginMock in _mockPlugins)
//            {
//                builder.Services.AddSingleton(pluginMock.Object);
//            }

//            builder.Services.AddAuthorization();
//            builder.Services.AddAuthentication();

//            // Build the application
//            var app = builder.Build();

//            // Enable routing and authorization middleware
//            app.UseRouting();
//            app.UseAuthentication();
//            app.UseAuthorization();

//            // Act
//            // Map the endpoints
//            var endpoints = JobEndpoints.MapJobEndpoints(app);

//            // Assert
//            // Verify that the correct number of endpoints were returned
//            Assert.Equal(7, endpoints.Count());

//            // Get the DataSource that contains the endpoints
//            var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();
//            var registeredEndpoints = endpointDataSource.Endpoints.ToList();

//            // Verify that the endpoints were registered
//            Assert.Equal(7, registeredEndpoints.Count);

//            // Check that each endpoint exists by name
//            var endpointNames = new HashSet<string>
//            {
//                "GetJobs", "GetJob", "TriggerJob",
//                "GetPlugins", "GetPlugin",
//                "GetJobHistory", "GetJobHistoryByName"
//            };

//            foreach (var endpoint in registeredEndpoints)
//            {
//                var routeEndpoint = endpoint as RouteEndpoint;
//                Assert.NotNull(routeEndpoint);

//                // Check that the route pattern starts with /api/jobs
//                Assert.StartsWith("/api/jobs", routeEndpoint.RoutePattern.RawText);

//                // Verify the endpoint name (using metadata)
//                var endpointNameMetadata = routeEndpoint.Metadata.OfType<IEndpointNameMetadata>().FirstOrDefault();
//                Assert.NotNull(endpointNameMetadata);
//                Assert.Contains(endpointNameMetadata.EndpointName, endpointNames);
//            }
//        }
//    }
//}
