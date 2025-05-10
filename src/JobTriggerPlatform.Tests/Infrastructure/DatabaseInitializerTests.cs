//using JobTriggerPlatform.Infrastructure.Persistence;
//using JobTriggerPlatform.Tests.Helpers;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Infrastructure;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using Moq;
//using Xunit;

//namespace JobTriggerPlatform.Tests.Infrastructure
//{
//    public class DatabaseInitializerTests
//    {
//        [Fact]
//        public async Task InitializeDatabaseAsync_CallsMigrateAsync()
//        {
//            // Arrange
//            var mockContext = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
//            var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
//            var mockDatabaseFacadeDependencies = new Mock<IDatabaseFacadeDependencies>();

//            mockContext.Setup(m => m.Database).Returns(mockDatabase.Object);
//            mockDatabase.Setup(d => d.GetService<IDatabaseFacadeDependencies>())
//                .Returns(mockDatabaseFacadeDependencies.Object);

//            var mockLogger = new Mock<ILogger<ApplicationDbContext>>();

//            var serviceProvider = new Mock<IServiceProvider>();
//            var serviceScope = new Mock<IServiceScope>();
//            var serviceScopeFactory = new Mock<IServiceScopeFactory>();

//            serviceProvider.Setup(s => s.GetService(typeof(ApplicationDbContext)))
//                .Returns(mockContext.Object);
//            serviceProvider.Setup(s => s.GetService(typeof(ILogger<ApplicationDbContext>)))
//                .Returns(mockLogger.Object);

//            serviceScope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);
//            serviceScopeFactory.Setup(x => x.CreateScope()).Returns(serviceScope.Object);
//            serviceProvider.Setup(s => s.GetService(typeof(IServiceScopeFactory)))
//                .Returns(serviceScopeFactory.Object);

//            // Set up Database.MigrateAsync to complete successfully
//            mockDatabase.Setup(d => d.MigrateAsync(It.IsAny<CancellationToken>()))
//                .Returns(Task.CompletedTask);

//            // Set up ExecuteSqlRawAsync to complete successfully
//            mockDatabase.Setup(d => d.ExecuteSqlRawAsync(
//                    It.IsAny<string>(),
//                    It.IsAny<object[]>(),
//                    It.IsAny<CancellationToken>()))
//                .Returns(Task.FromResult(0));

//            // Act
//            await DatabaseInitializer.InitializeDatabaseAsync(serviceProvider.Object);

//            // Assert
//            mockContext.Verify(c => c.Database, Times.AtLeast(2));
//            mockDatabase.Verify(d => d.ExecuteSqlRawAsync(
//                It.Is<string>(sql => sql.Contains("CREATE SCHEMA")),
//                It.IsAny<object[]>(),
//                It.IsAny<CancellationToken>()), Times.Once);
//            mockDatabase.Verify(d => d.MigrateAsync(It.IsAny<CancellationToken>()), Times.Once);
//            serviceProvider.Verify(s => s.GetService(typeof(ApplicationDbContext)), Times.Once);
//            serviceProvider.Verify(s => s.GetService(typeof(ILogger<ApplicationDbContext>)), Times.Once);
//        }
//    }
//}
