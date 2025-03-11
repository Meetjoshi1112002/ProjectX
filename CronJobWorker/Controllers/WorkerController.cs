using CronJobWorker.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CronJobWorker.Controllers
{
    [Route("api")]
    [ApiController]
    public class WorkerController : ControllerBase
    {
        [HttpPost("inform-task")]
        public async Task<IActionResult> GetTaskAsynce(MessageDto _dto)
        {
            try
            {

                return Ok();
            }
            catch(Exception e)
            {
                return Ok();
            }
        }
    }
}
