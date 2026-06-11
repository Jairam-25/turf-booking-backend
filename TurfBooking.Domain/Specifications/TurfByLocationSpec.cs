using System;
using System.Linq.Expressions;
using Domain.Entities;

namespace Domain.Specifications
{
    public class TurfByLocationSpec : Specification<Turf>
    {
        public TurfByLocationSpec(string location)
        {
            Criteria = t => t.Location.ToLower().Contains(location.ToLower()) || 
                            t.State.ToLower().Contains(location.ToLower()) ||
                            t.City.ToLower().Contains(location.ToLower());
        }

        public override Expression<Func<Turf, bool>> Criteria { get; }
    }
}
