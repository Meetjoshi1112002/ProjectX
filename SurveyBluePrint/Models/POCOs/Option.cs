using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SurveyBluePrint.Models.POCOs
{
    public class Option
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } 


        [BsonElement("text")]
        public string Text { get; set; }  // Option text

        [BsonElement("isCorrect")]
        [BsonIgnoreIfNull]  // This ensures it's only stored in Quiz Questions
        public bool? IsCorrect { get; set; }  // True if correct in MCQ Quiz
    }
}
