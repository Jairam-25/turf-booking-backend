using Domain.Entities;
using Persistence.Context;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories
{
    public class TurfRepository : GenericRepository<Turf>, ITurfRepository
    {
        public TurfRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Turf?> ValidateIdAsync(int? id)
        {
            return await _context.Turfs
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        }
    }
}
