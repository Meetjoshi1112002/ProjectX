using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MasterNode.Models;
using MasterNode.Models.POCOs;
using MasterNode.Models.DTOs;
using MasterNode.Services;
using OnBoarding.Data;

[ApiController]
[Route("api/schedule-start")]
public class SurveyStartController : ControllerBase
{
    private readonly IMongoCollection<SurveySchema> _surveys;
    private readonly IMongoCollection<SurveyTask> _surveyTask;
    private readonly IMongoCollection<HeartBeat> _slaveService;
    public readonly AppDbContext _context;
    private readonly ApiServices _slaveConnection;

    public SurveyStartController(AppDbContext database, ApiServices apiServices)
    {
        _context = database;
        _surveys = _context.GetCollection<SurveySchema>("Survey");
        _surveyTask = _context.GetCollection<SurveyTask>("SurveyTasks");
        _slaveService = _context.GetCollection<HeartBeat>("HeartBeat");
        _slaveConnection = apiServices;
    }

    [HttpPatch("{surveyId}")]
    public async Task<IActionResult> ScheduleStart(string surveyId, [FromBody] DateTime startTime)
    {
        try
        {
            // Validate survey existence and status
            var surveyFilter = Builders<SurveySchema>.Filter.Where(s => s.Id == surveyId);
            var survey = await _surveys.Find(surveyFilter).FirstOrDefaultAsync();
            if (survey == null)
            {
                return NotFound(new ApiResponse { Success = false, Message = "Survey not found." });
            }
            else if (survey.Config.Status == SurveyStatus.Active)
            {
                return BadRequest(new ApiResponse { Success = false, Message = "Cannot schedule a start for an active survey." });
            }
            else if (survey.Config.Scheduling.EndTime.HasValue && survey.Config.Scheduling.EndTime < startTime)
            {
                return BadRequest(new ApiResponse { Success = false, Message = "Survey cannot start after it ended." });
            }
            else if (startTime < DateTime.UtcNow)
            {
                return BadRequest(new ApiResponse { Success = false, Message = "Invalid time of past " });
            }

                /// Check if the slave service is healthy
            var heartBeatFilter = Builders<HeartBeat>.Filter.Where(h => h.Id == "67ced78f39538b6a4d3792c2" && h.HealthStatus == HealthStatus.Healthy);
            var heartBeat = await _slaveService.Find(heartBeatFilter).FirstOrDefaultAsync();
            if (heartBeat == null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse { Success = false, Message = "Service not available. Please start manually." });
            }

            // Check for any existing pending schedule start task
            var taskFilter = Builders<SurveyTask>.Filter.Where(task => task.SurveyId == surveyId && task.TaskStatus == MasterNode.Models.POCOs.TaskStatus.Pending && task.TaskType == TaskType.ScheduleStart);
            var previousTask = await _surveyTask.Find(taskFilter).FirstOrDefaultAsync();

            // Create message for slave node
            var messageDto = new MessageDto
            {
                TaskType = TaskType.ScheduleStart,
                ScheduledStart = startTime,
                SurveyId = surveyId,
                PreviousJobId = previousTask?.JobId
            };

            var response = await _slaveConnection.SendCommand(messageDto);
            if (!response.Success)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse { Success = false, Message = "Service unavailable. Please try again later." });
            }

            string jobId = response.Data.GetString();

            if (previousTask == null)
            {
                // create a new Task
                var newTask = new SurveyTask
                {
                    JobId = jobId,
                    SurveyId = surveyId,
                    TaskStatus = MasterNode.Models.POCOs.TaskStatus.Pending,
                    TaskType = TaskType.ScheduleStart,
                    CreatedAt = DateTime.UtcNow
                };
                await _surveyTask.InsertOneAsync(newTask);
            }
            else
            {
                var updateTask = Builders<SurveyTask>.Update
                    .Set(t => t.JobId, jobId)
                    .Set(t => t.CreatedAt, DateTime.UtcNow);
                await _surveyTask.UpdateOneAsync(taskFilter, updateTask);
            }

            // Update survey scheduling time
            var update = Builders<SurveySchema>.Update
                .Set(s => s.Config.Scheduling.StartTime, startTime)
                .Set(s => s.Config.Status, SurveyStatus.Scheduled);
            await _surveys.UpdateOneAsync(surveyFilter, update);

            return Ok(new ApiResponse { Success = true, Data = survey });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse { Success = false, Message = "Internal server error."+ex.Message });
        }
    }
}
