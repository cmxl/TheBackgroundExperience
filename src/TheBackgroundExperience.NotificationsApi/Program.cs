using Microsoft.Extensions.Options;
using Serilog;
using StackExchange.Redis;
using TheBackgroundExperience.Application.Configuration;
using TheBackgroundExperience.Infrastructure.HealthChecks;
using TheBackgroundExperience.Infrastructure.Resilience;
using TheBackgroundExperience.NotificationsApi.Hubs;
using TheBackgroundExperience.NotificationsApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
	configuration
		.ReadFrom.Configuration(context.Configuration)
		.ReadFrom.Services(services);
});

// Add Redis connection
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")!;
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
	return ConnectionMultiplexer.Connect(redisConnectionString);
});
builder.Services.AddSingleton<IDatabase>(sp =>
{
	var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
	return multiplexer.GetDatabase();
});

// Add configuration
builder.Services.Configure<ResilienceConfig>(builder.Configuration.GetSection(ResilienceConfig.SectionName));
builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection(RabbitMqConfig.SectionName));

// Add resilience
builder.Services.AddSingleton<ResiliencePipelineFactory>();

// Add SignalR with Redis backplane
builder.Services.AddSignalR()
	.AddStackExchangeRedis(redisConnectionString, options =>
	{
		options.Configuration.ChannelPrefix = new RedisChannel("TheBackgroundExperience.Notifications.SignalR.", RedisChannel.PatternMode.Literal);
	});

// Add health checks
builder.Services.AddHealthChecks()
	.AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: new[] { "messaging", "rabbitmq" })
	.AddCheck<RedisHealthCheck>("redis", tags: new[] { "cache", "redis" });

// Add notification consumption service
builder.Services.AddHostedService<NotificationConsumerService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
	app.UseDeveloperExceptionPage();
}

// Configure CORS for cross-origin access
app.UseCors(policy =>
{
	policy.AllowAnyOrigin()
		  .AllowAnyMethod()
		  .AllowAnyHeader();
});

app.UseStaticFiles();
app.UseRouting();

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

app.MapControllers();
app.MapHub<StudentNotificationHub>("/hubs/notifications");

app.Run();