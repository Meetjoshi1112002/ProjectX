using System.ComponentModel.DataAnnotations;

namespace OnBoarding.Models.DTOs
{
    public class DTO02
    {
        [Required]
        [EmailAddress] // Standard built-in email validation
        public string Email { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Company name must be between 2 and 100 characters.", MinimumLength = 2)]
        public string Company { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Name must be between 2 and 50 characters.", MinimumLength = 2)]
        public string Name { get; set; }

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP code must be exactly 6 digits.")]
        public string OtpCode { get; set; }
    }
}
