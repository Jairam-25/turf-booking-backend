using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Common.Result;
using Application.DTOs;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Context;
using Xunit;

namespace TurfBooking.Tests
{
    [Collection("IntegrationTests")]
    public class BookingIntegrationTests
    {
        private readonly TurfApiFactory _factory;
        private readonly int _seededSlotId;
        private readonly int _seededSlotId2;
        private readonly int _alreadyBookedSlotId;

        public BookingIntegrationTests(TurfApiFactory factory)
        {
            _factory = factory;

            // Clean and seed the database for each test run to ensure isolation
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // Seed Turf
            var turf = new Turf { Name = "Wembley Turf", Location = "London", PricePerHour = 200 };
            db.Turfs.Add(turf);
            db.SaveChanges();

            // Seed Slots
            var slot1 = new Slot 
            { 
                StartTime = DateTime.UtcNow.AddHours(1), 
                EndTime = DateTime.UtcNow.AddHours(2), 
                IsBooked = false, 
                TurfId = turf.Id 
            };
            var slot2 = new Slot 
            { 
                StartTime = DateTime.UtcNow.AddHours(2), 
                EndTime = DateTime.UtcNow.AddHours(3), 
                IsBooked = true, 
                TurfId = turf.Id 
            };
            var slot3 = new Slot
            {
                StartTime = DateTime.UtcNow.AddHours(3),
                EndTime = DateTime.UtcNow.AddHours(4),
                IsBooked = false,
                TurfId = turf.Id
            };
            db.Slots.AddRange(slot1, slot2, slot3);
            db.SaveChanges();

            _seededSlotId = slot1.Id;
            _seededSlotId2 = slot3.Id;
            _alreadyBookedSlotId = slot2.Id;
        }

        private async Task<HttpClient> GetAuthenticatedClientAsync()
        {
            var client = _factory.CreateClient();
            var registerRequest = new RegisterRequestDto
            {
                Name = "Booker User",
                Email = "booker@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                PhoneNumber = "1112223334"
            };
            await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

            var loginRequest = new LoginRequestDto
            {
                EmailOrPhone = "booker@example.com",
                Password = "Password123!"
            };
            var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            var result = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
            var token = result!.Data!.Token;

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        [Fact]
        public async Task BookSlot_WithValidAvailableSlot_Returns200OkAndBookingDetails()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            var bookingRequest = new CreateBookingDto { SlotId = _seededSlotId };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/booking", bookingRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Message.Should().Be("Slot booked successfully");
            result.Data.Should().NotBeNull();

            // Verify anonymous type fields using JsonElement
            var bookingIdProp = result.Data.GetProperty("bookingId");
            bookingIdProp.GetInt32().Should().BeGreaterThan(0);

            var turfNameProp = result.Data.GetProperty("turfName");
            turfNameProp.GetString().Should().Be("Wembley Turf");
        }

        [Fact]
        public async Task GetMyBookings_ReturnsUserBookings()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            var bookingRequest = new CreateBookingDto { SlotId = _seededSlotId };
            
            // First book a slot to ensure we have a booking to fetch
            var bookResponse = await client.PostAsJsonAsync("/api/v1/booking", bookingRequest);
            bookResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Act
            var response = await client.GetAsync("/api/v1/booking/my");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Message.Should().Be("Bookings retrieved successfully");
            result.Data.ValueKind.Should().Be(JsonValueKind.Array);
            result.Data.GetArrayLength().Should().Be(1);

            var firstBooking = result.Data[0];
            firstBooking.GetProperty("turfName").GetString().Should().Be("Wembley Turf");
            firstBooking.GetProperty("location").GetString().Should().Be("London");
        }

        [Fact]
        public async Task BookSlot_WithAlreadyBookedSlot_Returns400BadRequest()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            var bookingRequest = new CreateBookingDto { SlotId = _alreadyBookedSlotId };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/booking", bookingRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("Slot is already booked");
        }

        [Fact]
        public async Task GetMyBookings_ReturnsBookingsSortedByBookingDateDescending()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            
            // Book first slot
            var bookResponse1 = await client.PostAsJsonAsync("/api/v1/booking", new CreateBookingDto { SlotId = _seededSlotId });
            bookResponse1.StatusCode.Should().Be(HttpStatusCode.OK);
            
            await Task.Delay(100); // Ensure a minor difference in booking date

            // Book second slot
            var bookResponse2 = await client.PostAsJsonAsync("/api/v1/booking", new CreateBookingDto { SlotId = _seededSlotId2 });
            bookResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

            // Act
            var response = await client.GetAsync("/api/v1/booking/my");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.ValueKind.Should().Be(JsonValueKind.Array);
            result.Data.GetArrayLength().Should().Be(2);

            // The second booking (more recent) should be first in the array
            var firstBookingInList = result.Data[0];
            var secondBookingInList = result.Data[1];

            var id1 = firstBookingInList.GetProperty("bookingId").GetInt32();
            var id2 = secondBookingInList.GetProperty("bookingId").GetInt32();
            id1.Should().BeGreaterThan(id2);
        }
    }
}
