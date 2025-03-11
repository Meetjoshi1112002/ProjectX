using MasterNode;

var builder = WebApplication.CreateBuilder(args);

// Create an instance of Startup
var startup = new Startup(builder.Configuration);

// Configure services
startup.ConfigureServices(builder.Services);

var app = builder.Build();

// Configure middleware
startup.Configure(app);

app.Run();
