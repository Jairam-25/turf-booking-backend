namespace Application.Model
{
    public class PagedResult<T>
    {
        // The actual list of items for this page
        public IEnumerable<T> Items { get; set; }
            = Enumerable.Empty<T>();

        // Total number of records in DB (without pagination)
        public int TotalCount { get; set; }

        // Current page number
        public int Page { get; set; }

        // Number of items per page
        public int PageSize { get; set; }

        // Total number of pages
        public int TotalPages =>
            (int)Math.Ceiling(
                (double)TotalCount / PageSize);

        // Is there a next page?
        public bool HasNextPage => Page < TotalPages;

        // Is there a previous page?
        public bool HasPreviousPage => Page > 1;
    }
}
