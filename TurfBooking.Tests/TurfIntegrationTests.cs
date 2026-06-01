using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Application.Common.Result;
using Application.DTOs;
using Application.Model;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Context;
using Xunit;

namespace TurfBooking.Tests
{
    [Collection("IntegrationTests")]
    public class TurfIntegrationTests
    {
        private readonly TurfApiFactory _factory;

        public TurfIntegrationTests(TurfApiFactory factory)
        {
            _factory = factory;

            // Clean and seed the database for each test run to ensure isolation
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // Seed Turf data
            var turf1 = new Turf { Name = "Old Trafford Turf", Location = "Manchester", PricePerHour = 100 };
            var turf2 = new Turf { Name = "Camp Nou Turf", Location = "Barcelona", PricePerHour = 150 };
            db.Turfs.AddRange(turf1, turf2);
            db.SaveChanges();
        }

        [Fact]
        public async Task Get_AllTurfs_Returns200OkAndPagedResult()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/v1/turf?page=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TurfResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Message.Should().Be("Turfs retrieved successfully");
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);

            var turfNames = result.Data.Items.Select(t => t.Name).ToList();
            turfNames.Should().Contain("Old Trafford Turf");
            turfNames.Should().Contain("Camp Nou Turf");
        }

        [Fact]
        public async Task GetById_WithNonExistentId_Returns404NotFound()
        {
            // Arrange
            var client = _factory.CreateClient();
            var nonExistentId = 9999;

            // Act
            var response = await client.GetAsync($"/api/v1/turf/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().ContainEquivalentOf("Turf not found");
        }
    }
}
