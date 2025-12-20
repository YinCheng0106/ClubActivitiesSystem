
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ClubActivitiesSystem.Models.Entities;

namespace ClubActivitiesSystem.Models.ViewModel
{
    public class CreateEventViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "請輸入標題")]
        [StringLength(200)]
        public string Title { get; set; } = default!;

        [Required(ErrorMessage = "請選擇社團")]
        public int ClubId { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [Required(ErrorMessage = "請輸入開始時間")]
        [DataType(DataType.DateTime)]
        public DateTime? StartTime { get; set; }

        [Required(ErrorMessage = "請輸入結束時間")]
        [DataType(DataType.DateTime)]
        public DateTime? EndTime { get; set; }

        [Required]
        [RegularExpression("Draft|Published|Archived", ErrorMessage = "狀態必須為 Draft / Published / Archived")]
        public string Status { get; set; } = "Draft";
        public object? Id { get; internal set; }
        public string? CreatedBy { get; internal set; }
        public DateTime CreatedAt { get; internal set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartTime.HasValue && EndTime.HasValue && StartTime.Value >= EndTime.Value)
            {
                yield return new ValidationResult("結束時間必須晚於開始時間。", new[] { nameof(EndTime) });
            }
        }
        public Event ToEntity(string currentUserId)
        {
            var startUtc = NormalizeToUtc(StartTime!.Value);
            var endUtc = NormalizeToUtc(EndTime!.Value);

            return new Event
            {
                ClubId = ClubId,
                Title = Title,
                Description = Description,
                Location = Location,
                StartTime = startUtc,
                EndTime = endUtc,
                Status = string.IsNullOrWhiteSpace(Status) ? "Draft" : Status,
                CreatedBy = currentUserId,
            };
        }

        private static DateTime NormalizeToUtc(DateTime dt)
        {
            // 若 Kind 是 Unspecified，視為 Local 再轉成 UTC；若是 Local/UTC 則正確轉換。
            return dt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime()
                : dt.ToUniversalTime();
        }
        public static CreateEventViewModel WithDefaults()
        {
            var nowLocal = DateTime.Now;
            return new CreateEventViewModel
            {
                StartTime = nowLocal.AddDays(1).Date.AddHours(19),
                EndTime = nowLocal.AddDays(1).Date.AddHours(21),
                Status = "Draft"
            };
        }
    }
}
