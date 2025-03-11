using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace SurveyBluePrint.Models.POCOs
{
    public class Question
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } 


        [BsonElement("questionIndex")]
        public int QuestionIndex { get; set; }  // Determines order of the question

        [BsonElement("text")]
        public string Text { get; set; }  // Question text

        [BsonElement("type")]
        public QuestionType Type { get; set; }  // Enum for question type

        [BsonElement("options")]
        [BsonIgnoreIfNull]
        public List<Option>? Options { get; set; }  // For MCQ & Checkboxes

        [BsonElement("dropDownItems")]
        [BsonIgnoreIfNull]
        public List<string>? DropDownItems { get; set; }  // For DropDown type questions

        [BsonElement("minRange")]
        [BsonIgnoreIfNull]
        public int? MinRange { get; set; }  // For Linear Scale

        [BsonElement("maxRange")]
        [BsonIgnoreIfNull]
        public int? MaxRange { get; set; }  // For Linear Scale

        [BsonElement("correctAnswer")]
        [BsonIgnoreIfNull]  // This ensures it's only stored in Quiz Questions
        public string? CorrectAnswer { get; set; }  // For Fill-in-the-Blank & MCQ Quiz

        [BsonElement("score")]
        [BsonIgnoreIfNull]  // This ensures it's only stored in Quiz Questions
        public int? Score { get; set; }  // Score value for quiz-type questions
    }

    public enum QuestionType
    {
        SingleSelectMCQ,  // Single choice MCQ
        CheckBoxes,        // Multiple choice MCQ
        DropDown,          // Dropdown list selection
        TextAnswer,        // Open-ended text response
        LinearScale,       // Range-based selection (e.g., 1-5)
        FillInTheBlank,    // Answer-based quiz question
        Date,              // Date selection
        Time               // Time selection
    }
}
