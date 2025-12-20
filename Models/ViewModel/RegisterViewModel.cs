namespace ClubActivitiesSystem.Models.ViewModel
{
    using System.ComponentModel.DataAnnotations;

    public class RegisterViewModel
    {
        [Required]
        public string Name { get; set; } = default!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        [Required]
        public string? PhoneNumber { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "密碼至少需要 6 字元")]
        public string Password { get; set; } = default!;

        [Required]
        [Compare("Password", ErrorMessage = "兩次密碼輸入不一致")]
        public string ConfirmPassword { get; set; } = default!;
    }
}
