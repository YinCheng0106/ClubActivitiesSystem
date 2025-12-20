namespace ClubActivitiesSystem.Models.ViewModel
{
    public class ClubListViewModel
    {
        public int Id { get; set; }

        public string ClubName { get; set; } = default!;

        public string? Description { get; set; }

        public string? ImagePath { get; set; }

        public int MemberCount { get; set; }
    }
}