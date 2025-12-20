namespace ClubActivitiesSystem.Models.ViewModel
{
    public class ClubApplicationViewModel
    {
        public int ApplicationId { get; set; }
        public string UserId { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
