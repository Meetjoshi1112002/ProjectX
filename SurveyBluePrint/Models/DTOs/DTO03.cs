using System.ComponentModel.DataAnnotations;

namespace SurveyBluePrint.Models.DTOs
{
    public class MessageDTO
    {
        [Required]
        public string SurveyId { get; set; }


    }
}
