var builder = WebApplication.CreateBuilder(args);

var startup = new Startup(builder.Configuration);

startup.ConfigureServices(builder.Services);

var app = builder.Build();

var env = app.Services.GetRequiredService<IWebHostEnvironment>(); // Get the env

startup.Configure(app, env); // Pass env

app.Run();
