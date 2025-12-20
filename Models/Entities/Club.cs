using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubActivitiesSystem.Models.Entities
{
    [Table("clubs")]
    public class Club
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string ClubName { get; set; } = default!;

        [Column("description")]
        public string? Description { get; set; }

        [Column("created_by")]
        public string CreatedBy { get; set; } = default!;

        public User CreatedByUser { get; set; } = default!;

        [Column("status")]
        public string Status { get; set; } = "Active";

        [Column("image_path")]
        public string? ImagePath { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        public ICollection<ClubMember> Members { get; set; } = new List<ClubMember>();
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }

}
