using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using OnBoarding.Data;
using SurveyBluePrint.Models;
using SurveyBluePrint.Models.DTOs;
using SurveyBluePrint.Models.POCOs;

namespace SurveyBluePrint.Controllers
{
    [Route("api/surveys")]
    [ApiController]
    public class SurveyBlueprintController : ControllerBase
    {
        private readonly IMongoCollection<SurveySchema> _surveys;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;

        public SurveyBlueprintController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _surveys = _context.GetCollection<SurveySchema>("Survey");
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<SurveyDto>>>> GetAllSurveys()
        {
            var surveys = await _surveys.Find(_ => true).ToListAsync();
            var surveyDtos = _mapper.Map<List<SurveyDto>>(surveys);

            return Ok(new ApiResponse<List<SurveyDto>>
            {
                Success = true,
                Data = surveyDtos,
                Message = $"Retrieved {surveyDtos.Count} surveys"
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<SurveyDto>>> GetSurveyById(string id)
        {
            var survey = await _surveys.Find(s => s.Id == id).FirstOrDefaultAsync();
            if (survey == null)
            {
                return NotFound(new ApiResponse<SurveyDto>
                {
                    Success = false,
                    Message = "Survey not found"
                });
            }

            var surveyDto = _mapper.Map<SurveyDto>(survey);

            return Ok(new ApiResponse<SurveyDto>
            {
                Success = true,
                Data = surveyDto,
                Message = "Survey retrieved successfully"
            });
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<SurveyDto>>> CreateSurvey([FromBody] SurveyDto surveyDto)
        {
            if (surveyDto == null)
            {
                return BadRequest(new ApiResponse<SurveyDto>
                {
                    Success = false,
                    Message = "Invalid survey data"
                });
            }

            // Validate business rules: Either Sections OR Questions, NEVER BOTH
            if ((surveyDto.Sections != null && surveyDto.Sections.Any()) &&
                (surveyDto.Questions != null && surveyDto.Questions.Any()))
            {
                return BadRequest(new ApiResponse<SurveyDto>
                {
                    Success = false,
                    Message = "Survey cannot have both Sections and Questions at the same time"
                });
            }

            // Map DTO to Schema
            var survey = _mapper.Map<SurveySchema>(surveyDto);

            // Generate IDs for all nested documents
            AssignIdsForCreation(survey);

            if (surveyDto.IsQuiz)
            {
                survey.Config.ResponseLimit.LimitType = ResponseLimitType.Single;
                survey.Config.ResponseLimit.TrackingMethod = TrackingMethod.UserId;
                survey.Config.AccessControl.AccessType = AccessType.Restricted;
                survey.Config.QuizDuration = 30; // minutes
            }
            // Insert Survey into MongoDB
            await _surveys.InsertOneAsync(survey);
            var createdSurvey = _mapper.Map<SurveyDto>(survey);

            return CreatedAtAction(
                nameof(GetSurveyById),
                new { id = survey.Id },
                new ApiResponse<SurveyDto>
                {
                    Success = true,
                    Data = createdSurvey,
                    Message = "Survey created successfully"
                });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<SurveyDto>>> UpdateSurvey(string id, [FromBody] SurveyDto surveyDto)
        {
            // Fetch the existing survey
            var existingSurvey = await _surveys.Find(s => s.Id == id).FirstOrDefaultAsync();
            if (existingSurvey == null)
            {
                return NotFound(new ApiResponse<SurveyDto>
                {
                    Success = false,
                    Message = "Survey not found"
                });
            }

            // Validate business rules: Either Sections OR Questions, NEVER BOTH
            if ((surveyDto.Sections != null && surveyDto.Sections.Any()) &&
                (surveyDto.Questions != null && surveyDto.Questions.Any()))
            {
                return BadRequest(new ApiResponse<SurveyDto>
                {
                    Success = false,
                    Message = "Survey cannot have both Sections and Questions at the same time"
                });
            }

            // Store the existing nested objects for later reference
            var existingSections = existingSurvey.Sections?.ToDictionary(s => s.Id) ?? new Dictionary<string, Section>();
            var existingQuestions = existingSurvey.Questions?.ToDictionary(q => q.Id) ?? new Dictionary<string, Question>();

            // Map the DTOs to the existing model
            _mapper.Map(surveyDto, existingSurvey);

            // Set the update timestamp
            existingSurvey.UpdatedAt = DateTime.UtcNow;

            // Preserve existing IDs and assign new IDs only to new nested documents
            PreserveExistingIdsOnUpdate(surveyDto, existingSurvey, existingSections, existingQuestions);

            // Update in MongoDB
            await _surveys.ReplaceOneAsync(s => s.Id == id, existingSurvey);
            var updatedSurvey = _mapper.Map<SurveyDto>(existingSurvey);

            return Ok(new ApiResponse<SurveyDto>
            {
                Success = true,
                Data = updatedSurvey,
                Message = "Survey updated successfully"
            });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteSurvey(string id)
        {
            var result = await _surveys.DeleteOneAsync(s => s.Id == id);
            if (result.DeletedCount == 0)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Survey not found"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Survey deleted successfully"
            });
        }

        #region Helper Methods

        /// <summary>
        /// Assigns new MongoDB IDs to all nested documents during creation
        /// </summary>
        private void AssignIdsForCreation(SurveySchema survey)
        {
            // Process multi-page survey sections
            if (survey.Sections != null && survey.Sections.Any())
            {
                foreach (var section in survey.Sections)
                {
                    // Assign ID to section
                    section.Id = ObjectId.GenerateNewId().ToString();

                    // Process questions in this section
                    if (section.Questions != null)
                    {
                        foreach (var question in section.Questions)
                        {
                            // Assign ID to question
                            question.Id = ObjectId.GenerateNewId().ToString();

                            // Validate and normalize question schema
                            ValidateQuestionSchema(question);

                            // Process options if needed
                            AssignIdsToOptions(question);
                        }
                    }
                }
            }

            // Process single-page survey questions
            if (survey.Questions != null && survey.Questions.Any())
            {
                foreach (var question in survey.Questions)
                {
                    // Assign ID to question
                    question.Id = ObjectId.GenerateNewId().ToString();

                    // Validate and normalize question schema
                    ValidateQuestionSchema(question);

                    // Process options if needed
                    AssignIdsToOptions(question);
                }
            }
        }

        /// <summary>
        /// Assigns IDs to options for questions that have them
        /// </summary>
        private void AssignIdsToOptions(Question question)
        {
            if ((question.Type == QuestionType.SingleSelectMCQ || question.Type == QuestionType.CheckBoxes) &&
                question.Options != null)
            {
                foreach (var option in question.Options)
                {
                    option.Id = ObjectId.GenerateNewId().ToString();
                }
            }
        }

        /// <summary>
        /// Preserves existing IDs during update and only assigns new IDs to new elements
        /// </summary>
        private void PreserveExistingIdsOnUpdate(
            SurveyDto surveyDto,
            SurveySchema existingSurvey,
            Dictionary<string, Section> existingSections,
            Dictionary<string, Question> existingQuestions)
        {
            // Process multi-page survey sections
            if (existingSurvey.Sections != null && existingSurvey.Sections.Any())
            {
                for (int i = 0; i < existingSurvey.Sections.Count; i++)
                {
                    var section = existingSurvey.Sections[i];
                    var sectionDto = surveyDto.Sections[i];

                    // Preserve existing ID or generate new one if it's a new section
                    if (string.IsNullOrEmpty(sectionDto.Id) || !existingSections.ContainsKey(sectionDto.Id))
                    {
                        section.Id = ObjectId.GenerateNewId().ToString();
                    }
                    else
                    {
                        section.Id = sectionDto.Id;
                    }

                    // Get existing questions for this section
                    var existingSectionQuestions = existingSections.TryGetValue(section.Id, out var existingSection)
                        ? existingSection.Questions?.ToDictionary(q => q.Id) ?? new Dictionary<string, Question>()
                        : new Dictionary<string, Question>();

                    // Process questions in this section
                    if (section.Questions != null)
                    {
                        for (int j = 0; j < section.Questions.Count; j++)
                        {
                            var question = section.Questions[j];
                            var questionDto = sectionDto.Questions[j];

                            // Preserve existing ID or generate new one
                            if (string.IsNullOrEmpty(questionDto.Id) ||
                                !existingSectionQuestions.ContainsKey(questionDto.Id))
                            {
                                question.Id = ObjectId.GenerateNewId().ToString();
                            }
                            else
                            {
                                question.Id = questionDto.Id;
                            }

                            // Validate and normalize question schema
                            ValidateQuestionSchema(question);

                            // Process options
                            PreserveOptionIds(question, questionDto, existingSectionQuestions);
                        }
                    }
                }
            }

            // Process single-page survey questions
            if (existingSurvey.Questions != null && existingSurvey.Questions.Any())
            {
                for (int i = 0; i < existingSurvey.Questions.Count; i++)
                {
                    var question = existingSurvey.Questions[i];
                    var questionDto = surveyDto.Questions[i];

                    // Preserve existing ID or generate new one
                    if (string.IsNullOrEmpty(questionDto.Id) || !existingQuestions.ContainsKey(questionDto.Id))
                    {
                        question.Id = ObjectId.GenerateNewId().ToString();
                    }
                    else
                    {
                        question.Id = questionDto.Id;
                    }

                    // Validate and normalize question schema
                    ValidateQuestionSchema(question);

                    // Process options
                    PreserveOptionIds(question, questionDto, existingQuestions);
                }
            }
        }

        /// <summary>
        /// Preserves existing option IDs and assigns new IDs only to new options
        /// </summary>
        private void PreserveOptionIds(
            Question question,
            QuestionDto questionDto,
            Dictionary<string, Question> existingQuestions)
        {
            if ((question.Type == QuestionType.SingleSelectMCQ || question.Type == QuestionType.CheckBoxes) &&
                question.Options != null)
            {
                var existingOptions = existingQuestions.TryGetValue(question.Id, out var existingQuestion)
                    ? existingQuestion.Options?.ToDictionary(o => o.Id) ?? new Dictionary<string, Option>()
                    : new Dictionary<string, Option>();

                for (int i = 0; i < question.Options.Count; i++)
                {
                    var option = question.Options[i];
                    var optionDto = questionDto.Options[i];

                    // Preserve existing ID or generate new one
                    if (string.IsNullOrEmpty(optionDto.Id) || !existingOptions.ContainsKey(optionDto.Id))
                    {
                        option.Id = ObjectId.GenerateNewId().ToString();
                    }
                    else
                    {
                        option.Id = optionDto.Id;
                    }
                }
            }
        }

        /// <summary>
        /// Validates and normalizes question schema based on question type
        /// </summary>
        private void ValidateQuestionSchema(Question question)
        {
            switch (question.Type)
            {
                case QuestionType.SingleSelectMCQ:
                case QuestionType.CheckBoxes:
                    question.Options ??= new List<Option>();
                    question.DropDownItems = null;
                    question.MinRange = null;
                    question.MaxRange = null;
                    break;

                case QuestionType.DropDown:
                    question.DropDownItems ??= new List<string>();
                    question.Options = null;
                    question.MinRange = null;
                    question.MaxRange = null;
                    break;

                case QuestionType.LinearScale:
                    question.MinRange ??= 0;
                    question.MaxRange ??= 10;
                    question.Options = null;
                    question.DropDownItems = null;
                    break;

                case QuestionType.FillInTheBlank:
                    question.CorrectAnswer ??= string.Empty;
                    question.Score ??= 0;
                    question.Options = null;
                    question.DropDownItems = null;
                    question.MinRange = null;
                    question.MaxRange = null;
                    break;

                case QuestionType.TextAnswer:
                case QuestionType.Date:
                case QuestionType.Time:
                    question.Options = null;
                    question.DropDownItems = null;
                    question.MinRange = null;
                    question.MaxRange = null;
                    question.CorrectAnswer = null;
                    question.Score = null;
                    break;
            }
        }

        #endregion
    }
}