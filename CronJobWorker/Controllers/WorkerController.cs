using CronJobWorker.Models.DTOs;
using CronJobWorker.Models;
using CronJobWorker.Services.Implementations;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace CronJobWorker.Controllers
{
    [Route("api")]
    [ApiController]
    public class WorkerController : ControllerBase
    {
        private readonly SurveyServiceProvider _serviceProvider;

        public WorkerController(SurveyServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [HttpPost("inform-task")]
        public IActionResult GetTaskAsync(MessageDto _dto)
        {
            try
            {
                if (_dto.TaskType == Models.POCOs.TaskType.ScheduleStart)
                {
                    if (_dto.ScheduledStart == null)
                    {
                        return BadRequest(new ApiResponse
                        {
                            Success = false,
                            Message = "ScheduledStart time is required for ScheduleStart task."
                        });
                    }

                    string jobId;

                    if (!string.IsNullOrEmpty(_dto.PreviousJobId))
                    {
                        // Delete previous job
                        BackgroundJob.Delete(_dto.PreviousJobId);
                        Console.WriteLine("job deleted");
                    }

                    // Schedule the real service method
                    jobId = BackgroundJob.Schedule(() => _serviceProvider.ScheduleStart(_dto.SurveyId), _dto.ScheduledStart.Value);

                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Message = "Task scheduled successfully.",
                        Data = jobId
                    });
                }

                else if (_dto.TaskType == Models.POCOs.TaskType.ScheduleEnd)
                {
                    if (_dto.ScheduledEnd == null)
                    {
                        return BadRequest(new ApiResponse
                        {
                            Success = false,
                            Message = "ScheduledEnd time is required for ScheduleEnd task."
                        });
                    }
                    string jobId;

                    if (!string.IsNullOrEmpty(_dto.PreviousJobId))
                    {
                        // Delete previous job
                        BackgroundJob.Delete(_dto.PreviousJobId);
                        Console.WriteLine("job deleted");
                    }
                }

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Unsupported TaskType."
                    });
            }
            catch (Exception e)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while processing the request.",
                    Data = e.Message
                });
            }
        }
    }
}
