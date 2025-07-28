using Serilog;
using TheBackgroundExperience.Application;
using TheBackgroundExperience.Infrastructure;
using TheBackgroundExperience.Infrastructure.Persistence;
using TheBackgroundExperience.WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
	configuration
		.ReadFrom.Configuration(context.Configuration)
		.ReadFrom.Services(services);
});

builder.Services
       .AddApplication(builder.Configuration)
       .AddInfrastructure(builder.Configuration)
       .AddWebApi(builder.Configuration);

var app = builder.Build();

// Initialize and seed database
await using (var scope = app.Services.CreateAsyncScope())
{
	var initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();
	await initializer.InitialiseAsync();
	await initializer.SeedAsync();
}

app.UseSerilogRequestLogging(x =>
{
	x.IncludeQueryInRequestPath = true;
});

if (app.Environment.IsDevelopment())
{
	app.UseDeveloperExceptionPage();
	app.MapOpenApi();
}
else
{
	app.UseHsts();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();