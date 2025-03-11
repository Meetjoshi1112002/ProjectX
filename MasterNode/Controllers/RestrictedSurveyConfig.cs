using AutoMapper;
using MasterNode.Models;
using MasterNode.Models.POCOs;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OnBoarding.Data;
using static MasterNode.Models.DTOs.DTO02;

namespace MasterNode.Controllers
{
    [Route("api/config")]
    [ApiController]
    public class RestrictedSurveyConfig : ControllerBase
    {
        public readonly AppDbContext _context;
        public readonly IMongoCollection<SurveySchema> _surveys;
        public readonly IMapper _mapper;

        public RestrictedSurveyConfig(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _surveys = _context.GetCollection<SurveySchema>("Survey");
            _mapper = mapper;
        }


        [HttpPatch("update-link/{surveyId}")]
        public async Task<IActionResult> UpdateLinkSetting(string surveyId, bool? RequireUniqueLink = null, int? LinkExpiryHours = null)
        {
            try
            {
                var filter = Builders<SurveySchema>.Filter.Where(s =>
                    s.Id == surveyId &&
                    s.Config.AccessControl.AccessType == AccessType.Restricted &&
                    (s.Config.Status == SurveyStatus.Draft || s.Config.Status == SurveyStatus.Scheduled));

                var updateDefinition = new List<UpdateDefinition<SurveySchema>>();

                if (RequireUniqueLink.HasValue)
                {
                    updateDefinition.Add(Builders<SurveySchema>.Update.Set(s => s.Config.AccessControl.RequireUniqueLink, RequireUniqueLink));
                }
                if (LinkExpiryHours.HasValue)
                {
                    updateDefinition.Add(Builders<SurveySchema>.Update.Set(s => s.Config.AccessControl.LinkExpiryHours, LinkExpiryHours));
                }

                if (!updateDefinition.Any())
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "No valid fields provided for update."
                    });
                }

                var update = Builders<SurveySchema>.Update.Combine(updateDefinition);

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
                        Message = "Survey must be restricted and not started yet to update link settings."
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Survey link settings updated successfully",
                    Data = updatedSurvey
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while updating link settings."+e.Message
                });
            }
        }


        [HttpPatch("add-user/{surveyId}")]
        public async Task<IActionResult> AddUser(string surveyId, UserDto _dto)
        {
            try
            {
                var filter = Builders<SurveySchema>.Filter.Where(s => s.Id == surveyId && s.Config.AccessControl.AccessType == AccessType.Restricted);

                var existingUserFilter = Builders<SurveySchema>.Filter.ElemMatch(
                    s => s.Config.AccessControl.AllowedUserIds,
                    u => u.UserId == _dto.UserId
                );

                var existingUser = await _surveys.Find(filter & existingUserFilter).FirstOrDefaultAsync();
                if (existingUser != null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "User already exists in the allowed list"
                    });
                }

                var update = Builders<SurveySchema>.Update.Push(s => s.Config.AccessControl.AllowedUserIds, new UserDetails
                {
                    UserId = _dto.UserId,
                    Email = _dto.Email
                });

                var updatedSurvey = await _surveys.FindOneAndUpdateAsync(
                    filter,
                    update,
                    new FindOneAndUpdateOptions<SurveySchema>
                    {
                        ReturnDocument = ReturnDocument.After // Return the updated document
                    });

                if (updatedSurvey == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Survey not found or not restricted"
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "User added successfully",
                    Data = updatedSurvey
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while adding the user." + e.Message,
                });
            }
        }

        [HttpPatch("remove-user/{surveyId}")]
        public async Task<IActionResult> RemoveUser(string surveyId, UserDto _dto)
        {
            try
            {
                var filter = Builders<SurveySchema>.Filter.Where(s => s.Id == surveyId && s.Config.AccessControl.AccessType == AccessType.Restricted);

                var update = Builders<SurveySchema>.Update.PullFilter(
                    s => s.Config.AccessControl.AllowedUserIds,
                    u => u.UserId == _dto.UserId
                );

                var updatedSurvey = await _surveys.FindOneAndUpdateAsync(
                    filter,
                    update,
                    new FindOneAndUpdateOptions<SurveySchema>
                    {
                        ReturnDocument = ReturnDocument.After // Return the updated document
                    });

                if (updatedSurvey == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Survey not found or not restricted"
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "User removed successfully",
                    Data = updatedSurvey
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while removing the user." + e.Message,
                });
            }
        }
    }
}
