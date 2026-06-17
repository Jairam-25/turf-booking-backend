using Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Promo.Queries
{
    public class ValidatePromoCodeResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public decimal DiscountPercentage { get; set; }
        public int? PromoOfferId { get; set; }
    }

    public class ValidatePromoCodeQuery : IRequest<ValidatePromoCodeResult>
    {
        public int UserId { get; set; }
        public string PromoCode { get; set; }
    }

    public class ValidatePromoCodeQueryHandler : IRequestHandler<ValidatePromoCodeQuery, ValidatePromoCodeResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ValidatePromoCodeQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ValidatePromoCodeResult> Handle(ValidatePromoCodeQuery request, CancellationToken cancellationToken)
        {
            var code = request.PromoCode?.Trim().ToUpper();
            if (string.IsNullOrEmpty(code))
            {
                return new ValidatePromoCodeResult { IsValid = false, Message = "Promo code is required." };
            }

            var promo = await _unitOfWork.PromoOffers.AsQueryable()
                .FirstOrDefaultAsync(p => p.PromoCode.ToUpper() == code, cancellationToken);

            if (promo == null)
            {
                return new ValidatePromoCodeResult { IsValid = false, Message = "Invalid promo code." };
            }

            if (!promo.IsActive)
            {
                return new ValidatePromoCodeResult { IsValid = false, Message = "Promo code is no longer active." };
            }

            if (promo.ExpiryDate.HasValue && promo.ExpiryDate.Value < DateTime.UtcNow)
            {
                return new ValidatePromoCodeResult { IsValid = false, Message = "Promo code has expired." };
            }

            var hasUsed = await _unitOfWork.PromoUsages.AsQueryable()
                .AnyAsync(u => u.UserId == request.UserId && u.PromoOfferId == promo.Id, cancellationToken);

            if (hasUsed)
            {
                return new ValidatePromoCodeResult { IsValid = false, Message = "You have already used this promo code." };
            }

            return new ValidatePromoCodeResult
            {
                IsValid = true,
                Message = "Promo code applied successfully",
                DiscountPercentage = promo.DiscountPercentage,
                PromoOfferId = promo.Id
            };
        }
    }
}
