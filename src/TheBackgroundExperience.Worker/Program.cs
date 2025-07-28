using Serilog;
using TheBackgroundExperience.Application;
using TheBackgroundExperience.Application.Common;
using TheBackgroundExperience.Infrastructure;
using TheBackgroundExperience.Infrastructure.Persistence;
using TheBackgroundExperience.Worker.Services;
using TheBackgroundExperience.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSerilog((sp, config) =>
{
	config.ReadFrom.Services(sp)
	      .ReadFrom.Configuration(builder.Configuration);
});

builder.Services.AddHostedService<StudentQueueWorker>();

var host = builder.Build();

// Initialize and seed database
await using (var scope = host.Services.CreateAsyncScope())
{
	var initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();
	await initializer.InitialiseAsync();
	await initializer.SeedAsync();
}

host.Run();