using DotNetEnv;
using EmailWorkerService;
using EmailWorkerService.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<EmailSender>();
Env.Load("C:\\Users\\HEMANT\\Desktop\\Vengence\\EmailWorkerService\\.env");
var host = builder.Build();
host.Run();
