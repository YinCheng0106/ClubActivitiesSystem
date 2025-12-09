namespace ClubActivitiesSystem.Models
{
  using System.ComponentModel.DataAnnotations;
  public class LoginViewModel
  {
    [Required(ErrorMessage = "Email不為空")]
    [EmailAddress]
    public required string Email { get; set; }
    [Required(ErrorMessage = "密碼不為空")]
    [DataType(DataType.Password)]
    public required string Password { get; set; }
    public bool RememberMe { get; set; }
  }
}