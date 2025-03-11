using System.ComponentModel.DataAnnotations;

namespace OnBoarding.Models.DTOs
{
    public class DTO01
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
