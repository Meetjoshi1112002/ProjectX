﻿using MongoDB.Bson.Serialization.Attributes;

namespace SurveyBluePrint.Models.POCOs
{
    public class SurveyConfiguration
    {
        [BsonElement("responseLimit")]
        public ResponseLimit ResponseLimit { get; set; }

        [BsonElement("quizDuration")]
        [BsonIgnoreIfNull]
        public int? QuizDuration { get; set; } // in minutes

        [BsonElement("accessControl")]
        public AccessControl AccessControl { get; set; }

        [BsonElement("scheduling")]
        public SchedulingConfig Scheduling { get; set; }

        [BsonElement("status")]
        public SurveyStatus Status { get; set; }  // Checked before requesting template

        public SurveyConfiguration()
        {
            ResponseLimit = new ResponseLimit();
            AccessControl = new AccessControl();
            Scheduling = new SchedulingConfig();
            Status = SurveyStatus.Draft;

        }
    }

    public class ResponseLimit
    {
        [BsonElement("limitType")]
        public ResponseLimitType LimitType { get; set; }

        [BsonElement("trackingMethod")]
        public TrackingMethod TrackingMethod { get; set; }

        // Default value on template creation
        public ResponseLimit()
        {
            LimitType = ResponseLimitType.Multiple;
            TrackingMethod = TrackingMethod.None;
        }
    }

    public class AccessControl
    {
        [BsonElement("accessType")]

        public AccessType AccessType { get; set; } // for quiz always restricted !

        [BsonElement("allowedUserIds")]
        public List<UserDetails> AllowedUserIds { get; set; }

        [BsonElement("requireUniqueLink")]
        public bool RequireUniqueLink { get; set; }

        [BsonElement("linkExpiryHours")]
        [BsonIgnoreIfNull]
        public int? LinkExpiryHours { get; set; }

        [BsonElement("reminders")]
        [BsonIgnoreIfNull]
        public ReminderSettings Reminders { get; set; }

        public AccessControl()
        {
            AccessType = AccessType.Unrestricted;
            AllowedUserIds = new List<UserDetails>(); //empty
            RequireUniqueLink = false;
            LinkExpiryHours = null;
            Reminders = new ReminderSettings();
        }
    }

    public class UserDetails
    {
        [BsonElement("userId")]
        [BsonRequired]
        public string UserId { get; set; }
        [BsonElement("email")]
        [BsonRequired]
        public string Email { get; set; }
    }
    public class ReminderSettings
    {
        [BsonElement("enabled")] // then send the 
        public bool Enabled { get; set; }

        [BsonElement("intervals")] //exists only if enable
        public int? IntervalHours { get; set; }

        public ReminderSettings()
        {
            Enabled = false;
            IntervalHours = null;
        }
    }

    public class SchedulingConfig
    {
        [BsonElement("startTime")]
        public DateTime? StartTime { get; set; }

        [BsonElement("endTime")]
        public DateTime? EndTime { get; set; }

    }

    public enum ResponseLimitType
    {
        Single,
        Multiple
    }

    public enum TrackingMethod
    {
        None,

        Email, //  For Unrestricted survey with single response only

        UserId, // For Restricted Survey (single / mulitple)

        Cookie // Opitonal for both when single responses needed
    }

    public enum AccessType
    {
        Unrestricted,
        Restricted
    }

    public enum SurveyStatus
    {
        Draft, // imp
        Scheduled, // imp
        Active, //imp
        Paused,
        Completed, //imp
        Archived
    }
}
