using Serilog;
using StackExchange.Redis;
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

// Add SignalR with Redis backplane
builder.Services.AddSignalR()
	.AddStackExchangeRedis(redisConnectionString, options =>
	{
		options.Configuration.ChannelPrefix = new RedisChannel("TheBackgroundExperience.Notifications.SignalR.", RedisChannel.PatternMode.Literal);
	});

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

app.MapControllers();
app.MapHub<StudentNotificationHub>("/hubs/notifications");

app.Run();