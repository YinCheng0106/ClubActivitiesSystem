namespace ClubActivitiesSystem.Models.ViewModel
{
    public class ClubPagedResultViewModel
    {
        public List<ClubListViewModel> Clubs { get; set; } = new();

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }

        public int TotalCount { get; set; }
        public int TotalPages =>
            (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
