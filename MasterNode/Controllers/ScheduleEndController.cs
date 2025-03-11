using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MasterNode.Models;
using MasterNode.Models.POCOs;
using MasterNode.Models.DTOs;
using MasterNode.Services;
using OnBoarding.Data;

[ApiController]
[Route("api/schedule-end")]
public class SurveyEndController : ControllerBase
{
    private readonly IMongoCollection<SurveySchema> _surveys;
    private readonly IMongoCollection<SurveyTask> _surveyTask;
    private readonly IMongoCollection<HeartBeat> _slaveService;
    public readonly AppDbContext _context;
    private readonly ApiServices _slaveConnection;

    public SurveyEndController(AppDbContext database, ApiServices apiServices)
    {
        _context = database;
        _surveys = _context.GetCollection<SurveySchema>("Survey");
        _surveyTask = _context.GetCollection<SurveyTask>("SurveyTasks");
        _slaveService = _context.GetCollection<HeartBeat>("HeartBeat");
        _slaveConnection = apiServices;
    }

    [HttpPatch("{surveyId}")]
    public async Task<IActionResult> ScheduleEnd(string surveyId, DateTime endTime)
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
            else if (survey.Config.Status == SurveyStatus.Completed)
            {
                return BadRequest(new ApiResponse { Success = false, Message = "Cannot schedule end a survey which is completed." });
            }
            if (survey.Config.Scheduling.StartTime.HasValue && survey.Config.Scheduling.StartTime > endTime)
            {
                return BadRequest(new ApiResponse { Success = false, Message = "Survey cannot end before it starts." });
            }


            // Check if the slave service is healthy
            var heartBeatFilter = Builders<HeartBeat>.Filter.Where(h => h.Id == "67ced78f39538b6a4d3792c2");
            var heartBeat = await _slaveService.Find(heartBeatFilter).FirstOrDefaultAsync();
            if (heartBeat == null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse { Success = false, Message = "Service not available. Please start manually." });
            }

            // Check for any existing pending schedule start task
            var taskFilter = Builders<SurveyTask>.Filter.Where(task => task.SurveyId == surveyId && task.TaskStatus == MasterNode.Models.POCOs.TaskStatus.Pending && task.TaskType == TaskType.ScheduleEnd);
            var previousTask = await _surveyTask.Find(taskFilter).FirstOrDefaultAsync();

            // Create message for slave node
            var messageDto = new MessageDto
            {
                TaskType = TaskType.ScheduleEnd,
                ScheduledEnd = endTime,
                SurveyId = surveyId,
                PreviousJobId = previousTask?.JobId
            };

            var response = await _slaveConnection.SendCommand(messageDto);
            if (!response.Success)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse { Success = false, Message = "Service unavailable. Please try again later." });
            }

            string jobId = response.Data.JobId;

            if (previousTask == null)
            {
                // create a new Task
                var newTask = new SurveyTask
                {
                    JobId = jobId,
                    SurveyId = surveyId,
                    TaskStatus = MasterNode.Models.POCOs.TaskStatus.Pending,
                    TaskType = TaskType.ScheduleEnd,
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
            var update = Builders<SurveySchema>.Update.Set(s => s.Config.Scheduling.EndTime, endTime);
            await _surveys.UpdateOneAsync(surveyFilter, update);

            return Ok(new ApiResponse { Success = true, Data = survey });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse { Success = false, Message = "Internal server error."+ex.Message });
        }
    }
}
