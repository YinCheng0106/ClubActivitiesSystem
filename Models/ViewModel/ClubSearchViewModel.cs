namespace ClubActivitiesSystem.Models.ViewModel
{
    public class ClubSearchViewModel
    {
        public string? Keyword { get; set; }
        public string SortBy { get; set; } = "created";

        public string SortOrder { get; set; } = "desc";

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
