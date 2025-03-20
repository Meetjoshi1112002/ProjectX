using ReponseManagement.Models.DTOs;
using ReponseManagement.Models.POCOs;

namespace ReponseManagement.Services
{
    public class DTOConverter
    {
        // DTOs to prevent sending answers
        public SurveyDTO ConvertToSurveyDTO(SurveySchema survey)
        {
            var dto = new SurveyDTO
            {
                Id = survey.Id,
                Name = survey.Name,
                Description = survey.Description,
                IsQuiz = survey.IsQuiz
            };

            // Convert sections if present
            if (survey.Sections != null && survey.Sections.Any())
            {
                dto.Sections = survey.Sections.Select(s => new SectionDTO
                {
                    Id = s.Id,
                    Title = s.Title,
                    SectionIndex = s.SectionIndex,
                    Questions = s.Questions.Select(q => ConvertToQuestionDTO(q, false)).ToList()
                }).ToList();
            }

            // Convert direct questions if present
            if (survey.Questions != null && survey.Questions.Any())
            {
                dto.Questions = survey.Questions.Select(q => ConvertToQuestionDTO(q, false)).ToList();
            }

            return dto;
        }

        public SurveyDTO ConvertToQuizDTO(SurveySchema survey)
        {
            var dto = new SurveyDTO
            {
                Id = survey.Id,
                Name = survey.Name,
                Description = survey.Description,
                IsQuiz = true
            };

            // Convert sections if present
            if (survey.Sections != null && survey.Sections.Any())
            {
                dto.Sections = survey.Sections.Select(s => new SectionDTO
                {
                    Id = s.Id,
                    Title = s.Title,
                    SectionIndex = s.SectionIndex,
                    Questions = s.Questions.Select(q => ConvertToQuestionDTO(q, true)).ToList()
                }).ToList();
            }

            // Convert direct questions if present
            if (survey.Questions != null && survey.Questions.Any())
            {
                dto.Questions = survey.Questions.Select(q => ConvertToQuestionDTO(q, true)).ToList();
            }

            return dto;
        }

        private QuestionDTO ConvertToQuestionDTO(Question question, bool isQuiz)
        {
            var dto = new QuestionDTO
            {
                Id = question.Id,
                Text = question.Text,
                Type = question.Type,
                QuestionIndex = question.QuestionIndex,
                MinRange = question.MinRange,
                MaxRange = question.MaxRange,
                Score = isQuiz ? question.Score : null
            };

            // Convert options without correct answer flag
            if (question.Options != null && question.Options.Any())
            {
                dto.Options = question.Options.Select(o => new OptionDTO
                {
                    Id = o.Id,
                    Text = o.Text
                    // Deliberately omit IsCorrect field
                }).ToList();
            }

            // Add dropdown items if present
            dto.DropDownItems = question.DropDownItems;

            return dto;
        }
    }
}
