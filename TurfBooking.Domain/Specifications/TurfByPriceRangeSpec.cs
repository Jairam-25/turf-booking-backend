using System;
using System.Linq.Expressions;
using Domain.Entities;

namespace Domain.Specifications
{
    public class TurfByPriceRangeSpec : Specification<Turf>
    {
        public TurfByPriceRangeSpec(decimal? minPrice, decimal? maxPrice)
        {
            Criteria = t => (!minPrice.HasValue || t.PricePerHour >= minPrice.Value) &&
                            (!maxPrice.HasValue || t.PricePerHour <= maxPrice.Value);
        }

        public override Expression<Func<Turf, bool>> Criteria { get; }
    }
}
