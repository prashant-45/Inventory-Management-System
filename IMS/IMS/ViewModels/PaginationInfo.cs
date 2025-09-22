namespace IMS.ViewModels
{
    public class PaginationInfo
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

        // Helper properties for display
        public int StartRecord => (Page - 1) * PageSize + 1;
        public int EndRecord => Math.Min(Page * PageSize, TotalItems);
    }
}