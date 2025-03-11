using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SurveyBluePrint.Models.POCOs
{
    public class SurveySchema
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }  // Survey/Quiz Template Name

        [BsonElement("description")]
        public string? Description { get; set; }  // Optional description

        [BsonElement("isQuiz")]
        public bool IsQuiz { get; set; }  // True if it's a Quiz, False if it's a Survey

        [BsonElement("sections")]
        public List<Section>? Sections { get; set; }  // Sections (for multi-page surveys/quizzes)

        [BsonElement("questions")]
        public List<Question>? Questions { get; set; }  // Only for single-page surveys/quizzes

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // Default timestamp

        [BsonElement("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [BsonElement("configurations")]
        public SurveyConfiguration Config { get; set; } = new SurveyConfiguration();
    }
}
