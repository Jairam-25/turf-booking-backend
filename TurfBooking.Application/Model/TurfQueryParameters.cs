using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Model
{
    public class TurfQueryParameters
    {
        private const int MaxPageSize = 50;

        // Page number — default is 1
        public int Page { get; set; } = 1;

        // Clamp PageSize between 1 and 50
        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize
                ? MaxPageSize : value < 1
                ? 1 : value;
        }

        // Filter by location (optional)
        public string? Location { get; set; }

        // Filter by max price per hour (optional)
        public decimal? MaxPrice { get; set; }

        // Filter by min price per hour (optional)
        public decimal? MinPrice { get; set; }

        // Sort by: "price", "name" (optional)
        public string? SortBy { get; set; }

        // Sort direction: "asc" or "desc"
        public string SortOrder { get; set; } = "asc";
    }
}
