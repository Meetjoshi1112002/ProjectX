using CronJobWorker.Models.POCOs;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using OnBoarding.Data;
using SurveyBluePrint.Models.POCOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CronJobWorker.Services.Implementations
{
    public class SurveyServiceProvider
    {
        private readonly AppDbContext _context;
        private readonly IMongoCollection<SurveySchema> _surveys;
        private readonly IMongoCollection<SurveyTask> _surveyTask;
        private readonly ILogger<ServiceProvider> _logger;
        private readonly KafkaProducerService _kafkaProducer;

        public SurveyServiceProvider(
            AppDbContext context,
            ILogger<ServiceProvider> logger,
            KafkaProducerService kafkaProducer)
        {
            _context = context;
            _surveys = _context.GetCollection<SurveySchema>("Survey");
            _surveyTask = _context.GetCollection<SurveyTask>("SurveyTasks");
            _logger = logger;
            _kafkaProducer = kafkaProducer;
        }

        public async Task ScheduleStart(string surveyId)
        {
            var filter = Builders<SurveySchema>.Filter.Where(s => s.Id == surveyId);
            var update = Builders<SurveySchema>.Update
                .Set(s => s.Config.Status, SurveyStatus.Active);

            var options = new FindOneAndUpdateOptions<SurveySchema>
            {
                ReturnDocument = ReturnDocument.After
            };

            var updatedSurvey = await _surveys.FindOneAndUpdateAsync(filter, update, options);

            if (updatedSurvey == null)
            {
                _logger.LogError($"Survey {surveyId} not found or update failed.");
                return;
            }

            // If unrestricted, we're done here.
            _logger.LogInformation($"Survey {surveyId}, activation complete.");

            if (updatedSurvey.Config.AccessControl.AccessType == AccessType.Restricted)
            {
                
                // Now handling restricted survey email logic.
                List<UserDetails> users = updatedSurvey.Config.AccessControl.AllowedUserIds;
                if (users == null || users.Count == 0)
                {
                    _logger.LogWarning($"Survey {surveyId} is restricted but has no allowed users.");
                    return;
                }

                bool isUniqueLinkRequired = updatedSurvey.Config.AccessControl.RequireUniqueLink;
                int? expiryHours = updatedSurvey.Config.AccessControl.LinkExpiryHours;

                List<EmailBody> emails = new List<EmailBody>();

                foreach (UserDetails user in users)
                {
                    string token;
                    string info;
                    Template emailTemplate;

                    if (isUniqueLinkRequired)
                    {
                        token = _tokenCreator(surveyId, user.UserId, expiryHours);
                        emailTemplate = Template.UniqueLink;
                        info = $"This is a one-time invite unique link with expiry of {expiryHours} hours.";
                    }
                    else
                    {
                        token = _tokenCreator(surveyId); /// simialry ot noral survey
                        emailTemplate = Template.Invite;
                        info = "Link to fill the survey.";
                    }

                    emails.Add(new EmailBody
                    {
                        Email = user.Email,
                        Link = $"https://localhost:7890/api/form/{token}",
                        Template = emailTemplate,
                        Info = info
                    });
                }

                // Send batch emails via Kafka
                await _kafkaProducer.SendBatchMessagesAsync(emails);

                _logger.LogInformation($"Survey {surveyId} emails sent successfully.");
            }


            /// Mark the task compeleted
            var filterTask = Builders<SurveyTask>.Filter.Where(s => s.SurveyId == surveyId && s.TaskType == TaskType.ScheduleStart);
            var updateTask = Builders<SurveyTask>.Update
                .Set(s => s.TaskStatus, Models.POCOs.TaskStatus.Completed);
            await _surveyTask.FindOneAndUpdateAsync(filterTask, updateTask);
            return;
        }

        private string _tokenCreator(string surveyId, string? userId = null, int? expiryHours = null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("ThisIsAStaticLongEnoughSecretKeyForJWTGeneration123!");

            var claims = new List<Claim>
            {
                new Claim("surveyId", surveyId)
            };

            if (!string.IsNullOrEmpty(userId))
            {
                claims.Add(new Claim("userId", userId));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiryHours.HasValue ? DateTime.UtcNow.AddHours(expiryHours.Value) : null,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }



    }
}