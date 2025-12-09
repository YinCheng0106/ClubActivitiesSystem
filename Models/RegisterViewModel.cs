namespace ClubActivitiesSystem.Models
{
  using System.ComponentModel.DataAnnotations;
  public class RegisterViewModel
  {
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    public required string Email { get; set; }
    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public required string Password { get; set; }
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public required string ConfirmPassword { get; set; }
  }
}

