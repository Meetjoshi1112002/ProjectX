using AutoMapper;
using MasterNode.Models;
using MasterNode.Models.POCOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OnBoarding.Data;
using static MasterNode.Models.DTOs.DTO02;

namespace MasterNode.Controllers
{
    [Route("api/basic-config")]
    [ApiController]
    public class SurveyConfigController : ControllerBase
    {
        public readonly AppDbContext _context;
        public readonly IMongoCollection<SurveySchema> _surveys;
        public readonly IMapper _mapper;

        public SurveyConfigController(AppDbContext context,IMapper mapper)
        {
            _context = context;
            _surveys = _context.GetCollection<SurveySchema>("Survey");
            _mapper = mapper;
        }

        [HttpPatch("{surveyId}")] // This is nothing with workder // its for response management
        public async Task<IActionResult> UpdateSurveyConfig(string surveyId,ResposneLimitDto _dto)
        {
            try
            {
                var filter = Builders<SurveySchema>.Filter
                    .Where(s => s.Id == surveyId &&
                    (s.Config.Status == SurveyStatus.Draft || s.Config.Status == SurveyStatus.Scheduled));


                // Determine tracking method based on access control
                TrackingMethod trackingMethod = TrackingMethod.None;

                if (_dto.LimitType == ResponseLimitType.Single)
                {
                    trackingMethod = (_dto.AccessType == AccessType.Unrestricted)
                        ? TrackingMethod.Email
                        : TrackingMethod.UserId;
                }

                var update = Builders<SurveySchema>.Update
                    .Set(s => s.Config.ResponseLimit.LimitType, _dto.LimitType)
                    .Set(s => s.Config.ResponseLimit.TrackingMethod, trackingMethod)
                    .Set(s => s.Config.AccessControl.AccessType, _dto.AccessType);

                // Use FindOneAndUpdateAsync to return the updated survey
                var updatedSurvey = await _surveys.FindOneAndUpdateAsync(
                    filter,
                    update,
                    new FindOneAndUpdateOptions<SurveySchema>
                    {
                        ReturnDocument = ReturnDocument.After // Returns the updated document
                    });

                if (updatedSurvey == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Failed to update the survey"
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Response limit updated successfully",
                    Data = updatedSurvey
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while updating response limit. "+ e.Message,
                });
            }
        }

        [HttpPatch("~/quiz-config/{surveyId}")]
        public async Task<IActionResult> UpdateQuizConfig(string surveyId, int duration)
        {
            try
            {
                var filter = Builders<SurveySchema>.Filter.Where(s => s.Id == surveyId && s.IsQuiz &&
                (s.Config.Status == SurveyStatus.Draft || s.Config.Status == SurveyStatus.Scheduled));

                var update = Builders<SurveySchema>.Update
                    .Set(s => s.Config.QuizDuration, duration);

                var updatedSurvey = await _surveys.FindOneAndUpdateAsync(
                    filter,
                    update,
                    new FindOneAndUpdateOptions<SurveySchema>
                    {
                        ReturnDocument = ReturnDocument.After // Return the updated document
                    });

                if (updatedSurvey == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Survey not found or not a quiz"
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Quiz duration updated successfully",
                    Data = updatedSurvey
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while updating quiz duration."+e.Message
                });
            }
        }

        
    }
}
