using Application.Features.Promo.DTOs;
using Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Promo.Queries
{
    public class GetPromoOffersQuery : IRequest<List<PromoOfferDto>>
    {
        public int UserId { get; set; }
    }

    public class GetPromoOffersQueryHandler : IRequestHandler<GetPromoOffersQuery, List<PromoOfferDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetPromoOffersQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<PromoOfferDto>> Handle(GetPromoOffersQuery request, CancellationToken cancellationToken)
        {
            var offers = await _unitOfWork.PromoOffers.Query()
                .Where(p => p.IsActive)
                .ToListAsync(cancellationToken);

            var usages = await _unitOfWork.PromoUsages.Query()
                .Where(u => u.UserId == request.UserId)
                .Select(u => u.PromoOfferId)
                .ToListAsync(cancellationToken);

            var dtos = offers.Select(o => new PromoOfferDto
            {
                Id = o.Id,
                Title = o.Title,
                PromoCode = o.PromoCode,
                Description = o.Description,
                DiscountPercentage = o.DiscountPercentage,
                ExpiryDate = o.ExpiryDate,
                IsUsed = usages.Contains(o.Id)
            }).ToList();

            return dtos;
        }
    }
}
