using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace CronJobWorker.Models.POCOs
{
    public class SurveyTask
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("surveyId")]
        public string SurveyId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [BsonElement("jobId")]
        public string JobId { get; set; }

        [BsonElement("taskType")]
        public TaskType TaskType { get; set; }

        [BsonElement("taskStatus")]
        public TaskStatus TaskStatus { get; set; }

    }

    public enum TaskType
    {
        ScheduleStart,
        ScheduleEnd,
        Reminders
    }

    public enum TaskStatus
    {
        Pending,
        Completed,
        Failed,
        Aborted
    }

    public class HeartBeat
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonElement("lastUpdatedBeat")]
        public DateTime LastUpdatedBeat { get; set; }
        [BsonElement("healthStatus")]
        public HealthStatus HealthStatus { get; set; }
    }
    public enum HealthStatus
    {
        Dead,
        Healthy
    }
}
