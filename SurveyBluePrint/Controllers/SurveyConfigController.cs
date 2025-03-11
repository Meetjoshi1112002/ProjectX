using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OnBoarding.Data;
using SurveyBluePrint.Models;
using SurveyBluePrint.Models.DTOs;
using SurveyBluePrint.Models.POCOs;

namespace SurveyBluePrint.Controllers
{
    [Route("api/survey-config")]
    [ApiController]
    public class SurveyConfigController : ControllerBase
    {
        private readonly IMongoCollection<SurveySchema> _surveys;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;

        public SurveyConfigController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _surveys = _context.GetCollection<SurveySchema>("Survey");
            _mapper = mapper;
        }

        [HttpGet("{surveyId}")]
        public async Task<ActionResult<ApiResponse<SurveyConfigurationDto>>> GetSurveyConfig(string surveyId)
        {
            var survey = await _surveys.Find(s => s.Id == surveyId).FirstOrDefaultAsync();
            if (survey == null)
            {
                return NotFound(new ApiResponse<SurveyConfigurationDto>
                {
                    Success = false,
                    Message = "Survey not found"
                });
            }

            var configDto = _mapper.Map<SurveyConfigurationDto>(survey.Config);

            return Ok(new ApiResponse<SurveyConfigurationDto>
            {
                Success = true,
                Data = configDto,
                Message = "Survey configuration retrieved successfully"
            });
        }

        [HttpPut("{surveyId}")]
        public async Task<ActionResult<ApiResponse<SurveyConfigurationDto>>> UpdateSurveyConfig(
            string surveyId,
            [FromBody] SurveyConfigurationDto configDto)
        {
            // Validate input
            if (configDto == null)
            {
                return BadRequest(new ApiResponse<SurveyConfigurationDto>
                {
                    Success = false,
                    Message = "Invalid configuration data"
                });
            }

            // Find the survey
            var filter = Builders<SurveySchema>.Filter.Eq(s => s.Id, surveyId);
            var survey = await _surveys.Find(filter).FirstOrDefaultAsync();

            if (survey == null)
            {
                return NotFound(new ApiResponse<SurveyConfigurationDto>
                {
                    Success = false,
                    Message = "Survey not found"
                });
            }

            // Map the DTO to the configuration
            var updatedConfig = _mapper.Map<SurveyConfiguration>(configDto);

            // Update and save
            var update = Builders<SurveySchema>.Update
                .Set(s => s.Config, updatedConfig)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await _surveys.UpdateOneAsync(filter, update);

            return Ok(new ApiResponse<SurveyConfigurationDto>
            {
                Success = true,
                Data = configDto,
                Message = "Survey configuration updated successfully"
            });
        }

        [HttpPatch("{surveyId}/status")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateSurveyStatus(
            string surveyId,
            [FromBody] StatusUpdateDto statusUpdate)
        {
            // Find the survey
            var filter = Builders<SurveySchema>.Filter.Eq(s => s.Id, surveyId);
            var survey = await _surveys.Find(filter).FirstOrDefaultAsync();

            if (survey == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Survey not found"
                });
            }

            // Validate status transition
            if (!IsValidStatusTransition(survey.Config.Status, statusUpdate.Status))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Invalid status transition from {survey.Config.Status} to {statusUpdate.Status}"
                });
            }

            // Update status
            var update = Builders<SurveySchema>.Update
                .Set(s => s.Config.Status, statusUpdate.Status)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await _surveys.UpdateOneAsync(filter, update);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = $"Survey status updated to {statusUpdate.Status}"
            });
        }

        [HttpPatch("{surveyId}/scheduling")]
        public async Task<ActionResult<ApiResponse<SchedulingConfigDto>>> UpdateScheduling(
            string surveyId,
            [FromBody] SchedulingConfigDto schedulingDto)
        {
            // Find the survey
            var filter = Builders<SurveySchema>.Filter.Eq(s => s.Id, surveyId);
            var survey = await _surveys.Find(filter).FirstOrDefaultAsync();

            if (survey == null)
            {
                return NotFound(new ApiResponse<SchedulingConfigDto>
                {
                    Success = false,
                    Message = "Survey not found"
                });
            }

            // Validate scheduling
            if (schedulingDto.StartTime.HasValue && schedulingDto.EndTime.HasValue)
            {
                if (schedulingDto.StartTime >= schedulingDto.EndTime)
                {
                    return BadRequest(new ApiResponse<SchedulingConfigDto>
                    {
                        Success = false,
                        Message = "Start time must be before end time"
                    });
                }
            }

            // Update scheduling configuration
            var scheduling = _mapper.Map<SchedulingConfig>(schedulingDto);
            var update = Builders<SurveySchema>.Update
                .Set(s => s.Config.Scheduling, scheduling)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            // If we're setting a schedule, update status to Scheduled
            if (schedulingDto.StartTime.HasValue && survey.Config.Status == SurveyStatus.Draft)
            {
                update = update.Set(s => s.Config.Status, SurveyStatus.Scheduled);
            }

            await _surveys.UpdateOneAsync(filter, update);

            return Ok(new ApiResponse<SchedulingConfigDto>
            {
                Success = true, 
                Data = schedulingDto,
                Message = "Survey scheduling updated successfully"
            });
        }

        [HttpPatch("{surveyId}/access-control")]
        public async Task<ActionResult<ApiResponse<AccessControlDto>>> UpdateAccessControl(
            string surveyId,
            [FromBody] AccessControlDto accessControlDto)
        {
            // Find the survey
            var filter = Builders<SurveySchema>.Filter.Eq(s => s.Id, surveyId);
            var survey = await _surveys.Find(filter).FirstOrDefaultAsync();

            if (survey == null)
            {
                return NotFound(new ApiResponse<AccessControlDto>
                {
                    Success = false,
                    Message = "Survey not found"
                });
            }

            // Update access control
            var accessControl = _mapper.Map<AccessControl>(accessControlDto);
            var update = Builders<SurveySchema>.Update
                .Set(s => s.Config.AccessControl, accessControl)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await _surveys.UpdateOneAsync(filter, update);

            return Ok(new ApiResponse<AccessControlDto>
            {
                Success = true,
                Data = accessControlDto,
                Message = "Survey access control updated successfully"
            });
        }

        [HttpPatch("{surveyId}/response-limit")]
        public async Task<ActionResult<ApiResponse<ResponseLimitDto>>> UpdateResponseLimit(
            string surveyId,
            [FromBody] ResponseLimitDto responseLimitDto)
        {
            // Find the survey
            var filter = Builders<SurveySchema>.Filter.Eq(s => s.Id, surveyId);
            var survey = await _surveys.Find(filter).FirstOrDefaultAsync();

            if (survey == null)
            {
                return NotFound(new ApiResponse<ResponseLimitDto>
                {
                    Success = false,
                    Message = "Survey not found"
                });
            }

            // Update response limit
            var responseLimit = _mapper.Map<ResponseLimit>(responseLimitDto);
            var update = Builders<SurveySchema>.Update
                .Set(s => s.Config.ResponseLimit, responseLimit)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await _surveys.UpdateOneAsync(filter, update);

            return Ok(new ApiResponse<ResponseLimitDto>
            {
                Success = true,
                Data = responseLimitDto,
                Message = "Survey response limit updated successfully"
            });
        }

        #region Helper Methods

        private bool IsValidStatusTransition(SurveyStatus currentStatus, SurveyStatus newStatus)
        {
            // Define valid state transitions
            switch (currentStatus)
            {
                case SurveyStatus.Draft:
                    return newStatus == SurveyStatus.Active || newStatus == SurveyStatus.Scheduled || newStatus == SurveyStatus.Archived;

                case SurveyStatus.Scheduled:
                    return newStatus == SurveyStatus.Active || newStatus == SurveyStatus.Draft || newStatus == SurveyStatus.Archived;

                case SurveyStatus.Active:
                    return newStatus == SurveyStatus.Paused || newStatus == SurveyStatus.Completed || newStatus == SurveyStatus.Archived;

                case SurveyStatus.Paused:
                    return newStatus == SurveyStatus.Active || newStatus == SurveyStatus.Completed || newStatus == SurveyStatus.Archived;

                case SurveyStatus.Completed:
                    return newStatus == SurveyStatus.Archived;

                case SurveyStatus.Archived:
                    return false; // Archived is a terminal state

                default:
                    return false;
            }
        }

        #endregion
    }

    // Additional DTOs for specific operations
    public class StatusUpdateDto
    {
        public SurveyStatus Status { get; set; }
    }
}