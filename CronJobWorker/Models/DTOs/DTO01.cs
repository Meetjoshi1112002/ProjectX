using CronJobWorker.Models.POCOs;
using System.ComponentModel.DataAnnotations;

namespace CronJobWorker.Models.DTOs
{
    public class MessageDto
    {
        [Required]
        public string SurveyId { get; set; }

        public string? PreviousJobId { get; set; } // if this is present then we will need to overwrite previosu job setting

        [Required]
        public TaskType TaskType { get; set; } // if it is remainder then surveyId is enough infromation

        // If Task is scheduled Start
        public DateTime? ScheduledStart { get; set; }

        // If Task is Schedulted End this field will be active 
        public DateTime? ScheduledEnd { get; set; }

        // If for the Reaminder task
        public int? RemainderInterval { get; set; }
    }

    public class SlaveResponse
    {
        [Required]
        public string JobId { get; set; }
    }
}
