using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Domain.Specifications
{
    public abstract class Specification<T>
    {
        public abstract Expression<Func<T, bool>> Criteria { get; }
        
        public List<Expression<Func<T, object>>> Includes { get; } = new();
    }
}
