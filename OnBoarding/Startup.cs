using OnBoarding.Data;
using OnBoarding.Services;

namespace OnBoarding
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
            services.AddAutoMapper(typeof(MappingProfile)); // Add AutoMapper
            services.AddScoped<AppDbContext>(); 
            services.AddScoped<MongoDbTestService>();
            services.AddScoped<JWTHelper>();
            services.AddScoped<KafkaProducerService>();

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
