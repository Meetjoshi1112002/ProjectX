using CronJobWorker.Models.POCOs;
using Microsoft.Extensions.Primitives;
using MongoDB.Driver;
using OnBoarding.Data;
using SurveyBluePrint.Models.POCOs;

namespace CronJobWorker.Services.Implementations
{
    public class ServiceProvider
    {
        private readonly AppDbContext _context;
        private readonly IMongoCollection<SurveySchema> _surveys;
        private readonly IMongoCollection<SurveyTask> _surveyTask;
        private readonly ILogger<ServiceProvider> _logger;
        private readonly KafkaProducerService _kafkaProducer;

        public ServiceProvider(
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

            if(updatedSurvey == null)
            {
                var filterTask = Builders<SurveyTask>.Filter.Where(t => t.SurveyId == surveyId && t.TaskStatus == Models.POCOs.TaskStatus.Pending && t.TaskType == TaskType.ScheduleStart);
                var updateTask = Builders<SurveyTask>.Update
                    .Set(t => t.TaskStatus, Models.POCOs.TaskStatus.Failed);
                return;
            }

            /// if the survey is restricted then we need to send email to some allowed user as well
            if(updatedSurvey.Config.AccessControl.AccessType == AccessType.Restricted)
            {
                List<UserDetails> users = updatedSurvey.Config.AccessControl.AllowedUserIds;
                if(users.Count > 0)
                {
                    List<EmailBody> emails = new List<EmailBody>();
                    EmailBody email = new();
                    foreach(UserDetails user in users)
                    {
                        string token;
                        /// if unique link is requrired or not
                        if (updatedSurvey.Config.AccessControl.RequireUniqueLink)
                        {
                            token = _tokerCreator(surveyId, user.UserId, updatedSurvey.Config.AccessControl.LinkExpiryHours);
                            email.Template = Template.UniqueLink;
                            email.Info = "This is a one time invite unqiue link with expiry of " + updatedSurvey.Config.AccessControl.LinkExpiryHours
                        }
                        else
                        {
                            token = _tokenCreator(surveyId);
                            email.Template = Template.Invite;
                            email.Info = "Link to fill the survey";
                        }
                        email.Email = user.Email;
                        email.Link = "https://localhost:7890/survey/" + token;
                        emails.Add(email);
                    }
                }
            }

            return;

        }
    }
}