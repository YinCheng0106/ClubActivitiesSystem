namespace ClubActivitiesSystem.Models
{
  using System.ComponentModel.DataAnnotations;
  public class RegisterViewModel
  {
    [Required(ErrorMessage = "Email 不為空")]
    [EmailAddress]
    public required string Email { get; set; }
    [Required(ErrorMessage = "密碼不為空")]
    [DataType(DataType.Password)]
    public required string Password { get; set; }
    [Compare("Password", ErrorMessage = "密碼不匹配")]
    public required string ConfirmPassword { get; set; }
  }
}

