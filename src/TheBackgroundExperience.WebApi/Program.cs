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

// Add health checks endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
	ResponseWriter = async (context, report) =>
	{
		context.Response.ContentType = "application/json";
		var response = new
		{
			status = report.Status.ToString(),
			checks = report.Entries.Select(x => new
			{
				name = x.Key,
				status = x.Value.Status.ToString(),
				exception = x.Value.Exception?.Message,
				duration = x.Value.Duration.ToString(),
				data = x.Value.Data
			})
		};
		await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
	}
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
	Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
	Predicate = _ => false
});

app.MapControllers();
app.Run();