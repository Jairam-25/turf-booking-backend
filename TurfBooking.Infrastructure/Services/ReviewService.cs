using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReviewService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<ReviewResponseDto>> CreateReviewAsync(CreateReviewDto dto, int userId, CancellationToken ct = default)
        {
            // Check if Turf exists
            var turf = await _unitOfWork.Turfs.GetByIdAsync(dto.TurfId, ct);
            if (turf == null)
            {
                return Result<ReviewResponseDto>.Failure("Turf not found");
            }

            // Check if user has booked this turf
            var hasBooked = await _unitOfWork.Bookings.AsQueryable()
                .Include(b => b.Slot)
                .AnyAsync(b => b.UserId == userId && b.Slot != null && b.Slot.TurfId == dto.TurfId, ct);

            if (!hasBooked)
            {
                return Result<ReviewResponseDto>.Failure("Only users who have booked this turf can leave a review");
            }

            // Check if already reviewed
            var alreadyReviewed = await _unitOfWork.Reviews.AsQueryable()
                .AnyAsync(r => r.UserId == userId && r.TurfId == dto.TurfId, ct);

            if (alreadyReviewed)
            {
                return Result<ReviewResponseDto>.Failure("You have already reviewed this turf");
            }

            if (dto.Rating < 1 || dto.Rating > 5)
            {
                return Result<ReviewResponseDto>.Failure("Rating must be between 1 and 5 stars");
            }

            var review = new Review
            {
                TurfId = dto.TurfId,
                UserId = userId,
                Rating = dto.Rating,
                Comment = dto.Comment ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Reviews.AddAsync(review, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            // Fetch user info for DTO
            var user = await _unitOfWork.Users.GetByIdAsync(userId, ct);

            var responseDto = new ReviewResponseDto
            {
                Id = review.Id,
                TurfId = review.TurfId,
                UserId = review.UserId,
                UserName = user?.Name ?? "Anonymous",
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            };

            return Result<ReviewResponseDto>.Success(responseDto);
        }

        public async Task<Result<IEnumerable<ReviewResponseDto>>> GetReviewsByTurfAsync(int turfId, CancellationToken ct = default)
        {
            var reviews = await _unitOfWork.Reviews.AsQueryable()
                .Include(r => r.User)
                .Where(r => r.TurfId == turfId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewResponseDto
                {
                    Id = r.Id,
                    TurfId = r.TurfId,
                    UserId = r.UserId,
                    UserName = r.User != null ? r.User.Name : "Anonymous",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync(ct);

            return Result<IEnumerable<ReviewResponseDto>>.Success(reviews);
        }
    }
}
