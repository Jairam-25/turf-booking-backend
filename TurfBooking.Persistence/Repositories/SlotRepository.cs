using Domain.Entities;
using Persistence.Context;
using Application.Interfaces;

namespace Persistence.Repositories
{
    public class SlotRepository : GenericRepository<Slot>, ISlotRepository
    {
        public SlotRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
