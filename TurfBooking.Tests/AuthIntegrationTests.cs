using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Application.Common.Result;
using Application.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Context;
using Xunit;

namespace TurfBooking.Tests
{
    public class AuthIntegrationTests : IClassFixture<TurfApiFactory>
    {
        private readonly TurfApiFactory _factory;

        public AuthIntegrationTests(TurfApiFactory factory)
        {
            _factory = factory;
            
            // Clean the database for each test run to ensure isolation
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }
        
        [Fact]
        public async Task Register_WithValidData_Returns200Ok()
        {
            // Arrange
            var client = _factory.CreateClient();
            var registerRequest = new RegisterRequestDto
            {
                Name = "John Doe",
                Email = "john.doe@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                PhoneNumber = "1234567890"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Registration successful", result.Message);
        }

        [Fact]
        public async Task Login_WithIncorrectPassword_Returns401Unauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // First, register a user so they exist
            var registerRequest = new RegisterRequestDto
            {
                Name = "Jane Doe",
                Email = "jane.doe@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                PhoneNumber = "9876543210"
            };
            await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

            var loginRequest = new LoginRequestDto
            {
                EmailOrPhone = "jane.doe@example.com",
                Password = "WrongPassword123!"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("email or password", result.Message, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetBookingMy_WithoutToken_Returns401Unauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/v1/booking/my");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
