using Domain.Common;
using System;

namespace Domain.Entities
{
    public class PromoUsage : BaseEntity
    {
        public int UserId { get; set; }
        public int PromoOfferId { get; set; }
        public int BookingId { get; set; }
        public DateTime UsedDate { get; set; }

        public User User { get; set; }
        public PromoOffer PromoOffer { get; set; }
        public Booking Booking { get; set; }
    }
}
