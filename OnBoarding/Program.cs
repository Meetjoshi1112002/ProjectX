using OnBoarding;

var builder = WebApplication.CreateBuilder(args);

//builder.WebHost.UseKestrel()
//    .UseUrls("http://localhost:0"); // 0 means auto-assign a free port

// Create an instance of Startup
var startup = new Startup(builder.Configuration);

// Configure services
startup.ConfigureServices(builder.Services);

var app = builder.Build();

// Configure middleware
startup.Configure(app);

app.Run();
