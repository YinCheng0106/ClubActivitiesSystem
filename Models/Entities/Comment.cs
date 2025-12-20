using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubActivitiesSystem.Models.Entities
{
    [Table("comments")]
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        public int EventId { get; set; }
        public string UserId { get; set; } = default!;

        public string? Title { get; set; }
        public string Description { get; set; } = default!;
        public string Status { get; set; } = "Visible";

        public DateTime CreatedAt { get; set; }
    }

}
