using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OnBoarding.Data;
using OnBoarding.Models;
using OnBoarding.Models.DTOs;
using OnBoarding.Models.POCOs;
using OnBoarding.Services;

[Route("api/auth")]
[ApiController]
public class SignupController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<Otp> _otps;
    private readonly IMapper _mapper;
    private readonly KafkaProducerService _kafkaProducer;

    public SignupController(AppDbContext dbContext, IMapper mapper, KafkaProducerService kafkaProducer)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _kafkaProducer = kafkaProducer;
        _users = _dbContext.GetCollection<User>("Users");
        _otps = _dbContext.GetCollection<Otp>("Otps");
    }

    [HttpPost("req-register")]
    public async Task<IActionResult> RequestVerification([FromBody] DTO01 dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new Response
            {
                IsError = true,
                Message = "Validation failed",
                Data = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        var existingUser = await _users.Find(u => u.Email == dto.Email).FirstOrDefaultAsync();
        if (existingUser != null)
            return BadRequest(new Response { IsError = true, Message = "User already exists" });

        var otpCode = new Random().Next(100000, 999999).ToString();
        var filter = Builders<Otp>.Filter.Eq(o => o.Email, dto.Email);
        var update = Builders<Otp>.Update
            .Set(o => o.OtpCode, otpCode)
            .Set(o => o.OtpExpiry, DateTime.UtcNow.AddMinutes(3));

        var options = new FindOneAndUpdateOptions<Otp>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var result = await _otps.FindOneAndUpdateAsync(filter, update, options);

        // Handover to kafka to send the mail
        await _kafkaProducer.SendMessageAsync(new EmailBody()
        {
            Email = dto.Email,
            Template = Template.Validation,
            OtpCode = otpCode
        });

        return Ok(new Response { IsError = false, Message = "OTP has been generated and stored" });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] DTO02 dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new Response
            {
                IsError = true,
                Message = "Validation failed",
                Data = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        try
        {
            // Step 2: Use Atomic Update with FindOneAndDelete (Solves Race Condition)
            var otpRecord = await _otps.FindOneAndDeleteAsync(o => o.Email == dto.Email && o.OtpCode == dto.OtpCode && o.OtpExpiry >= DateTime.UtcNow);
            if (otpRecord == null)
            {
                return BadRequest(new Response { IsError = true, Message = "Invalid OTP or OTP expired" });
            }

            // Step 3: Map DTO to User entity
            var user = _mapper.Map<User>(dto);

            // Step 4: Insert user into Users collection
            await _users.InsertOneAsync(user);

            // Step 5: Send welcome email

            await _kafkaProducer.SendMessageAsync(new EmailBody()
            {
                Email = user.Email,
                Template = Template.Welcome,
                Name = user.Name
            });

            return Ok(new Response { IsError = false, Message = "User created successfully", Data = user });
        }
        catch (MongoException mongoEx)
        {
            // Handle MongoDB-specific errors
            return StatusCode(500, new Response { IsError = true, Message = "Database error", Data = mongoEx.Message });
        }
        catch (Exception ex)
        {
            // Handle any other unexpected errors
            return StatusCode(500, new Response { IsError = true, Message = "Something went wrong", Data = ex.Message });
        }
    }

}
