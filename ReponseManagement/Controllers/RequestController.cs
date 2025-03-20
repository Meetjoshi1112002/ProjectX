using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using ReponseManagement.Data;
using ReponseManagement.Models;
using ReponseManagement.Models.POCOs;
using ReponseManagement.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace ReponseManagement.Controllers
{
    [Route("api/form")]
    [ApiController]
    public class RequestController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMongoCollection<SurveySchema> _surveys;
        private readonly ILogger<RequestController> _logger;
        private readonly DTOConverter _converter;

        public RequestController(AppDbContext context, ILogger<RequestController> logger,DTOConverter converter)
        {
            _context = context;
            _surveys = _context.GetCollection<SurveySchema>("Survey");
            _logger = logger;
            _converter = converter;
        }

        [HttpGet("{token}")]
        public async Task<IActionResult> RequestForm(string token)
        {
            try
            {
                IDictionary<string, string> claims = DecodeToken(token);

                if (!claims.ContainsKey("surveyId"))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid token: surveyId claim is missing"
                    });
                }

                string surveyId = claims["surveyId"];

                // Find the survey
                var filter = Builders<SurveySchema>.Filter.Eq(s => s.Id, surveyId);
                var survey = await _surveys.Find(filter).FirstOrDefaultAsync();

                if (survey == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Survey not found"
                    });
                }

                // Check survey status
                if (survey.Config.Status != SurveyStatus.Active)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = $"Survey is not active. Current status: {survey.Config.Status}"
                    });
                }

                // Handle scheduling constraints
                if (survey.Config.Scheduling.StartTime.HasValue && DateTime.UtcNow < survey.Config.Scheduling.StartTime.Value)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = $"Survey not yet started. Starts at {survey.Config.Scheduling.StartTime.Value}"
                    });
                }

                if (survey.Config.Scheduling.EndTime.HasValue && DateTime.UtcNow > survey.Config.Scheduling.EndTime.Value)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Survey has ended"
                    });
                }

                // Process based on survey type
                if (survey.IsQuiz)
                {
                    return await HandleQuizRequest(claims, survey, filter);
                }
                else if (survey.Config.AccessControl.AccessType == AccessType.Restricted)
                {
                    return await HandleRestrictedSurveyRequest(claims, survey, filter);
                }
                else
                {
                    return HandleUnrestrictedSurveyRequest(survey);
                }
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Token has expired");
                return Unauthorized(new ApiResponse
                {
                    Success = false,
                    Message = "Token has expired"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request");
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Invalid token or server error"
                });
            }
        }

        private async Task<IActionResult> HandleQuizRequest(IDictionary<string, string> claims, SurveySchema survey, FilterDefinition<SurveySchema> filter)
        {
            // Verify user ID is present for quiz
            if (!claims.ContainsKey("userId"))
            {
                return Unauthorized(new ApiResponse
                {
                    Success = false,
                    Message = "User ID required for quiz access"
                });
            }

            string userId = claims["userId"];

            // Check if user is allowed to take the quiz
            if (!IsUserAllowed(userId, survey.Config.AccessControl.AllowedUserIds))
            {
                return Unauthorized(new ApiResponse
                {
                    Success = false,
                    Message = "User not authorized for this quiz"
                });
            }

            // Find existing attempt for this user
            var attemptEntry = survey.Config.AttemptedUsers
                .FirstOrDefault(u => u.UserId == userId);

            // Default quiz duration (in minutes)
            int quizDuration = survey.Config.QuizDuration ?? 60;

            // New attempt
            if (attemptEntry == null)
            {
                return await CreateNewQuizAttempt(userId, survey, filter, quizDuration);
            }

            // Check existing attempt status
            if (attemptEntry.SubmittedAt.HasValue)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Quiz already submitted"
                });
            }

            if (attemptEntry.Expired)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Quiz attempt has expired"
                });
            }

            // Check if attempt has now expired based on start time and duration
            TimeSpan elapsedTime = DateTime.UtcNow - attemptEntry.StartedAt;
            if (elapsedTime.TotalMinutes > quizDuration)
            {
                // Mark as expired
                var updateExpired = Builders<SurveySchema>.Update
                    .Set(s => s.Config.AttemptedUsers[-1].Expired, true);

                await _surveys.UpdateOneAsync(
                    filter & Builders<SurveySchema>.Filter.ElemMatch(
                        s => s.Config.AttemptedUsers,
                        a => a.UserId == userId),
                    updateExpired);

                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Quiz time has expired"
                });
            }

            // Calculate remaining time
            TimeSpan remainingTime = TimeSpan.FromMinutes(quizDuration) - elapsedTime;
            DateTime expiryTime = DateTime.UtcNow.Add(remainingTime);

            // Set quiz expiry cookie
            Response.Cookies.Append("QuizExpiry", expiryTime.ToString("o"), new CookieOptions
            {
                Expires = expiryTime,
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            // Return sanitized quiz content
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Quiz accessed successfully",
                Data = new
                {
                    quiz = _converter.ConvertToQuizDTO(survey),
                    remainingMinutes = Math.Floor(remainingTime.TotalMinutes)
                }
            });
        }

        private async Task<IActionResult> CreateNewQuizAttempt(string userId, SurveySchema survey, FilterDefinition<SurveySchema> filter, int quizDuration)
        {
            var firstAttempt = new AttemptStatus
            {
                UserId = userId,
                StartedAt = DateTime.UtcNow,
                Expired = false,
                SubmittedAt = null
            };

            var update = Builders<SurveySchema>.Update
                .Push(s => s.Config.AttemptedUsers, firstAttempt);

            var options = new FindOneAndUpdateOptions<SurveySchema>
            {
                ReturnDocument = ReturnDocument.After
            };

            var updatedSurvey = await _surveys.FindOneAndUpdateAsync(filter, update, options);

            // Calculate expiry time
            DateTime expiryTime = DateTime.UtcNow.AddMinutes(quizDuration);

            // Set quiz expiry cookie
            Response.Cookies.Append("QuizExpiry", expiryTime.ToString("o"), new CookieOptions
            {
                Expires = expiryTime,
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            // Return sanitized quiz content
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Quiz started",
                Data = new
                {
                    quiz = _converter.ConvertToQuizDTO(updatedSurvey),
                    durationMinutes = quizDuration
                }
            });
        }

        private async Task<IActionResult> HandleRestrictedSurveyRequest(IDictionary<string, string> claims, SurveySchema survey, FilterDefinition<SurveySchema> filter)
        {
            // Verify user ID is present for restricted survey
            if (!claims.ContainsKey("userId"))
            {
                return Unauthorized(new ApiResponse
                {
                    Success = false,
                    Message = "User ID required for restricted survey access"
                });
            }

            string userId = claims["userId"];

            // Check if user is allowed to take the survey
            if (!IsUserAllowed(userId, survey.Config.AccessControl.AllowedUserIds))
            {
                return Unauthorized(new ApiResponse
                {
                    Success = false,
                    Message = "User not authorized for this survey"
                });
            }

            // Check if user has already submitted (for single response limit)
            if (survey.Config.ResponseLimit.LimitType == ResponseLimitType.Single)
            {
                var attemptEntry = survey.Config.AttemptedUsers
                    .FirstOrDefault(u => u.UserId == userId && u.SubmittedAt.HasValue);

                if (attemptEntry != null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "You have already submitted a response to this survey"
                    });
                }
            }

            // Create tracking entry if not exists
            var existingAttempt = survey.Config.AttemptedUsers
                .FirstOrDefault(u => u.UserId == userId && !u.SubmittedAt.HasValue);

            if (existingAttempt == null)
            {
                var attempt = new AttemptStatus
                {
                    UserId = userId,
                    StartedAt = DateTime.UtcNow,
                    Expired = false
                };

                var update = Builders<SurveySchema>.Update
                    .Push(s => s.Config.AttemptedUsers, attempt);

                await _surveys.UpdateOneAsync(filter, update);
            }

            // Return survey content
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Survey accessed successfully",
                Data = new
                {
                    survey = _converter.ConvertToSurveyDTO(survey)
                }
            });
        }

        private IActionResult HandleUnrestrictedSurveyRequest(SurveySchema survey)
        {
            // If cookie tracking is used for single responses, check cookie
            if (survey.Config.ResponseLimit.LimitType == ResponseLimitType.Single &&
                survey.Config.ResponseLimit.TrackingMethod == TrackingMethod.Cookie)
            {
                string cookieKey = $"SurveySubmitted_{survey.Id}";
                if (Request.Cookies.ContainsKey(cookieKey))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "You have already submitted a response to this survey"
                    });
                }
            }

            // Return survey content
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Survey accessed successfully",
                Data = new
                {
                    survey = _converter.ConvertToSurveyDTO(survey)
                }
            });
        }

        private bool IsUserAllowed(string userId, List<UserDetails> allowedUsers)
        {
            return allowedUsers.Any(u => u.UserId == userId);
        }

        private IDictionary<string, string> DecodeToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("ThisIsAStaticLongEnoughSecretKeyForJWTGeneration123!");

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero // For stricter validation
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return principal.Claims.ToDictionary(c => c.Type, c => c.Value);
        }
    }
    
}