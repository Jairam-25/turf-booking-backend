using System;

namespace Application.Features.Promo.DTOs
{
    public class PromoOfferDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string PromoCode { get; set; }
        public string Description { get; set; }
        public decimal DiscountPercentage { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsUsed { get; set; }
    }
}
