using SurveyBluePrint.Models.POCOs;

namespace SurveyBluePrint.Models.DTOs;

public class SurveyDto
{
    public string? Id { get; set; }  // Nullable for new survey creation
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsQuiz { get; set; }
    public List<SectionDto>? Sections { get; set; }  // For multi-page surveys
    public List<QuestionDto>? Questions { get; set; }  // For single-page surveys
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SectionDto
{
    public string? Id { get; set; }
    public int SectionIndex { get; set; }
    public string Title { get; set; }
    public List<QuestionDto> Questions { get; set; } = new();
}

public class QuestionDto
{
    public string? Id { get; set; }
    public int QuestionIndex { get; set; }
    public string Text { get; set; }
    public QuestionType Type { get; set; }
    public List<OptionDto>? Options { get; set; }
    public List<string>? DropDownItems { get; set; }
    public int? MinRange { get; set; }
    public int? MaxRange { get; set; }
    public string? CorrectAnswer { get; set; }  // Only for Quiz
    public int? Score { get; set; }  // Only for Quiz
}

public class OptionDto
{
    public string? Id { get; set; }
    public string Text { get; set; }
    public bool? IsCorrect { get; set; }  // Only for Quiz
}