using JobTriggerPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;

namespace JobTriggerPlatform.Tests.Helpers
{
    public static class TestDbContextFactory
    {
        public static ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }
    }
}
