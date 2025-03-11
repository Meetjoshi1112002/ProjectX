using MasterNode.MappingProfile;
using MasterNode.Services;
using OnBoarding.Data;

namespace MasterNode
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<AppDbContext>();
            services.AddAutoMapper(typeof(SurveyProfile));
            services.AddScoped<ApiServices>();

            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }


        public void Configure(WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseAuthorization();
            app.MapControllers();
        }
    }
}
