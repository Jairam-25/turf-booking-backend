using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Application.Common.Result;
using Application.DTOs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Context;
using Xunit;

namespace TurfBooking.Tests
{
    [Collection("IntegrationTests")]
    public class AuthIntegrationTests
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Message.Should().Be("Registration successful");
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
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().ContainEquivalentOf("email or password");
        }

        [Fact]
        public async Task Login_WithCorrectCredentials_Returns200OkAndToken()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // First, register a user so they exist
            var registerRequest = new RegisterRequestDto
            {
                Name = "Login User",
                Email = "login.user@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                PhoneNumber = "9998887776"
            };
            var regResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
            regResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var loginRequest = new LoginRequestDto
            {
                EmailOrPhone = "login.user@example.com",
                Password = "Password123!"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Message.Should().Be("Login successful");
            result.Data.Should().NotBeNull();
            result.Data!.Token.Should().NotBeNullOrEmpty();
            result.Data.RefreshToken.Should().NotBeNullOrEmpty();
            result.Data.Email.Should().Be(registerRequest.Email);
            result.Data.Name.Should().Be(registerRequest.Name);
        }

        [Fact]
        public async Task Login_WithTooManyFailedAttempts_Returns429TooManyRequests()
        {
            // Arrange
            using var isolatedFactory = new TurfApiFactory();
            var client = isolatedFactory.CreateClient();
            var loginRequest = new LoginRequestDto
            {
                EmailOrPhone = "rate.limit@example.com",
                Password = "WrongPassword123!"
            };

            HttpResponseMessage? lastResponse = null;

            // Act & Assert
            // We make 6 attempts. The rate limit is 5 attempts per minute.
            for (int i = 1; i <= 6; i++)
            {
                var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
                if (i <= 5)
                {
                    // The first 5 requests should get 401 Unauthorized (due to bad credentials, but rate limit allows them)
                    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
                }
                else
                {
                    // The 6th request should hit the LoginPolicy rate limiter and return 429 Too Many Requests
                    lastResponse = response;
                }
            }

            // Assert 6th request is 429
            lastResponse.Should().NotBeNull();
            ((int)lastResponse!.StatusCode).Should().Be(429);
        }

        [Fact]
        public async Task GetBookingMy_WithoutToken_Returns401Unauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/v1/booking/my");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
