using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnBoarding.Services;

namespace OnBoarding.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly MongoDbTestService _testService;

        public TestController(MongoDbTestService testService)
        {
            _testService = testService;
        }

        [HttpGet("mongodb")]
        public IActionResult CheckMongoDB()
        {
            bool isConnected = _testService.IsDatabaseConnected();
            if (isConnected)
                return Ok("MongoDB connection is successful! 🎉");
            else
                return StatusCode(500, "MongoDB connection failed! ❌");
        }
    }
}
