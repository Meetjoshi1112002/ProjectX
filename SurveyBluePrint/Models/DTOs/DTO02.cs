using MongoDB.Bson.Serialization.Attributes;
using SurveyBluePrint.Models.POCOs;
using System.ComponentModel.DataAnnotations;

namespace SurveyBluePrint.Models.DTOs
{
    // Main configuration DTO
    public class SurveyConfigurationDto
    {
        public ResponseLimitDto ResponseLimit { get; set; } = new ResponseLimitDto();
        public AccessControlDto AccessControl { get; set; } = new AccessControlDto();
        public SchedulingConfigDto Scheduling { get; set; } = new SchedulingConfigDto();

        public int? QuizDuration { get; set; }
        public SurveyStatus Status { get; set; } = SurveyStatus.Draft;
    }

    // Response limit settings
    public class ResponseLimitDto
    {
        public ResponseLimitType LimitType { get; set; } = ResponseLimitType.Multiple;
        public TrackingMethod TrackingMethod { get; set; } = TrackingMethod.None;
    }

    public class UserDetailDto
    {
        [Required]
        public string UserId { get; set; }
        [Required]
        public string Email { get; set; }
    }

    // Access control settings
    public class AccessControlDto
    {
        public AccessType AccessType { get; set; } = AccessType.Unrestricted;
        public List<UserDetailDto> AllowedUserIds { get; set; } = new List<UserDetailDto>();
        public bool RequireUniqueLink { get; set; } = false;
        public int? LinkExpiryHours { get; set; } = null;
        public ReminderSettingsDto? Reminders { get; set; } = null;
    }

    // Reminder settings DTO should match the POCO structure
    public class ReminderSettingsDto
    {
        public bool Enabled { get; set; } = false;
        public int? IntervalHours { get; set; } = null; // Changed from List<int> to int?
    }

    // Scheduling configuration
    public class SchedulingConfigDto
    {
        public DateTime? StartTime { get; set; } = null;
        public DateTime? EndTime { get; set; } = null;
    }

    // Wrapper DTO for partial configuration updates
    public class ConfigUpdateRequestDto
    {
        public string SurveyId { get; set; } // ID of the survey to update
        public SurveyConfigurationDto Configuration { get; set; } = new SurveyConfigurationDto();
    }
}
