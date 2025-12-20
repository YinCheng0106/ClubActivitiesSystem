namespace ClubActivitiesSystem.Models.ViewModel
{
    public class ClubDetailViewModel
    {
        public int Id { get; set; }

        public string ClubName { get; set; } = default!;

        public string? Description { get; set; }

        public string Status { get; set; } = default!;

        public string CreatedByName { get; set; } = default!;

        public DateTime CreatedAt { get; set; }

        public int MemberCount { get; set; }

        public bool IsMember { get; set; }

        public bool IsAdmin { get; set; }
    }

}
