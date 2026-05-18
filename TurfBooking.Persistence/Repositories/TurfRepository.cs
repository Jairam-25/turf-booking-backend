using Domain.Entities;
using Persistence.Context;
using Application.Interfaces;

namespace Persistence.Repositories
{
    public class TurfRepository : GenericRepository<Turf>, ITurfRepository
    {
        public TurfRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
