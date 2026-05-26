using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Hubs;

namespace Infrastructure.Services
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<SlotHub> _hubContext;

        public BookingService(IUnitOfWork unitOfWork, IHubContext<SlotHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
        }

        public async Task<Result<object>> BookSlotAsync(CreateBookingDto dto, int userId, CancellationToken ct = default)
        {
            // Find slot with related turf
            var slot = await _unitOfWork.Slots.AsQueryable()
                .Include(s => s.Turf)
                .FirstOrDefaultAsync(s => s.Id == dto.SlotId, ct);

            if (slot == null)
                return Result<object>.Failure("Slot not found");

            if (slot.IsBooked)
                return Result<object>.Failure("Slot is already booked");

            var booking = new Booking
            {
                UserId = userId,
                SlotId = dto.SlotId,
                BookingDate = System.DateTime.UtcNow
            };

            // Add domain event
            booking.AddDomainEvent(new Domain.Events.BookingCreatedEvent(
                booking.Id,
                booking.UserId,
                booking.SlotId,
                booking.BookingDate
            ));

            slot.IsBooked = true;
            await _unitOfWork.Bookings.AddAsync(booking, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            // Notify connected clients viewing this turf that a slot was booked
            try
            {
                await _hubContext.Clients
                    .Group($"turf_{slot.TurfId}")
                    .SendAsync("SlotBooked", new {
                        slotId = slot.Id,
                        isBooked = true
                    });
            }
            catch
            {
                // If SignalR notification fails, don't break the booking flow
            }

            var data = new
            {
                bookingId = booking.Id,
                slotId = slot.Id,
                turfName = slot.Turf!.Name,
                location = slot.Turf.Location,
                startTime = slot.StartTime,
                endTime = slot.EndTime,
                bookedOn = booking.BookingDate
            };

            return Result<object>.Success(data);
        }

        public async Task<Result<object>> GetMyBookingsAsync(int userId, CancellationToken ct = default)
        {
            var bookings = await _unitOfWork.Bookings.AsQueryable()
                .Include(b => b.Slot)
                .ThenInclude(s => s!.Turf)
                .Where(b => b.UserId == userId)
                .Select(b => new
                {
                    bookingId = b.Id,
                    bookedOn = b.BookingDate,
                    turfName = b.Slot!.Turf!.Name,
                    location = b.Slot.Turf.Location,
                    price = b.Slot.Turf.PricePerHour,
                    startTime = b.Slot.StartTime,
                    endTime = b.Slot.EndTime
                })
                .ToListAsync(ct);

            return Result<object>.Success(bookings);
        }

        public async Task<Result<string>> CancelBookingAsync(int bookingId, int userId, CancellationToken ct = default)
        {
            var booking = await _unitOfWork.Bookings.AsQueryable()
                .Include(b => b.Slot)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId, ct);

            if (booking == null)
                return Result<string>.Failure("Booking not found");

            // Free the slot
            if (booking.Slot != null)
                booking.Slot.IsBooked = false;

            await _unitOfWork.Bookings.DeleteAsync(booking, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<string>.Success("Booking cancelled successfully");
        }
    }
}
