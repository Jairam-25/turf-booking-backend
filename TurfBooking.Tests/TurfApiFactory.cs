using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Persistence.Context;

namespace TurfBooking.Tests
{
    public class TurfApiFactory : WebApplicationFactory<Program>
    {
        public Mock<IEmailService> EmailServiceMock { get; } = new();
        
        private readonly string _dbName = Guid.NewGuid().ToString();
        
        private static readonly Mock<JobStorage> _mockStorage = new();
        private static readonly Mock<IStorageConnection> _mockConnection = new();

        static TurfApiFactory()
        {
            // Set static JobStorage.Current to our mock to prevent BackgroundJob.Enqueue from throwing exception
            _mockStorage.Setup(x => x.GetConnection()).Returns(_mockConnection.Object);
            JobStorage.Current = _mockStorage.Object;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                // 1. Replace Database Context with EF Core In-Memory database
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                });

                // 2. Replace Redis Distributed Cache with Memory Distributed Cache
                var redisDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IDistributedCache));
                if (redisDescriptor != null)
                {
                    services.Remove(redisDescriptor);
                }
                services.AddDistributedMemoryCache();

                // 3. Replace Email Service with Mock
                var emailServiceDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IEmailService));
                if (emailServiceDescriptor != null)
                {
                    services.Remove(emailServiceDescriptor);
                }
                
                EmailServiceMock.Setup(e => e.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.CompletedTask);
                EmailServiceMock.Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.CompletedTask);

                services.AddScoped(_ => EmailServiceMock.Object);

                // 4. Replace Hangfire JobStorage registration with our mock to prevent Hangfire initialization
                var jobStorageDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(JobStorage));
                if (jobStorageDescriptor != null)
                {
                    services.Remove(jobStorageDescriptor);
                }
                services.AddSingleton<JobStorage>(_mockStorage.Object);

                // 5. Remove all IHostedService registrations (which are Hangfire background servers in this app)
                var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
                foreach (var service in hostedServices)
                {
                    services.Remove(service);
                }
            });
        }
    }
}
