using System.ComponentModel.DataAnnotations;

namespace OnBoarding.Models.POCOs
{
    public class EmailBody
    {
        [Required]
        public string Email { get; set; } // email of person who we want to send (TO)

        public string? Name { get; set; } // Used when Welcome email Template

        public string? URL { get; set; }  // Url is req when template is forget password
        public string? Info { get; set; } // Additional info in case of expiry
        public string? Link { get; set; } //Link when template is invite for quiz

        public string? OtpCode { get; set; } // Opt code when template is verificaiton

        public Template Template { get; set; } // create subject and body based on template
    }

    public enum Template
    {
        Welcome,
        Validation,
        ForgetPassword,
        Invite,
        Remainder,
        Quiz,
        UniqueLink,
        Failure
    }
}
