using Domain.Entities;
using Persistence.Context;
using Application.Interfaces;

namespace Persistence.Repositories
{
    public class BookingRepository : GenericRepository<Booking>, IBookingRepository
    {
        public BookingRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
