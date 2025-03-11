using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace SurveyBluePrint.Models.POCOs
{
    public class Section
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } 


        [BsonElement("sectionIndex")]
        public int SectionIndex { get; set; }  // Section order

        [BsonElement("title")]
        public string Title { get; set; }  // Section Title

        [BsonElement("questions")]
        public List<Question> Questions { get; set; }  // Embedded list of questions
    }
}
