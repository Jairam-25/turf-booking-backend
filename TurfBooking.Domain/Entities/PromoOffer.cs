using Domain.Common;
using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class PromoOffer : BaseEntity
    {
        public string Title { get; set; }
        public string PromoCode { get; set; }
        public string Description { get; set; }
        public decimal DiscountPercentage { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; }

        public ICollection<PromoUsage> PromoUsages { get; set; } = new List<PromoUsage>();
    }
}
