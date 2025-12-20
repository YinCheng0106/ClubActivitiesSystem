using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubActivitiesSystem.Models.Entities
{
    [Table("event_registrations")]
    public class EventRegistration
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("event_id")]
        public int EventId { get; set; }

        public Event Event { get; set; } = default!;

        [Column("user_id")]
        public string UserId { get; set; } = default!;

        public User User { get; set; } = default!;

        [Column("registered_at")]
        public DateTime RegisteredAt { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Pending";

        [Column("payment_status")]
        public string PaymentStatus { get; set; } = "Unpaid";
    }

}
