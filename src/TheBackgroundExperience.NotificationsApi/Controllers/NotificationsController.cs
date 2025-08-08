using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using TheBackgroundExperience.Domain.Events.Notifications;

namespace TheBackgroundExperience.NotificationsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
	private readonly IDatabase _redisDatabase;
	private readonly ILogger<NotificationsController> _logger;

	public NotificationsController(
		IDatabase redisDatabase,
		ILogger<NotificationsController> logger)
	{
		_redisDatabase = redisDatabase;
		_logger = logger;
	}

	[HttpGet("students/stream")]
	public async Task<IActionResult> StreamStudentNotifications(CancellationToken cancellationToken)
	{
		Response.Headers.ContentType = "text/event-stream";
		Response.Headers.CacheControl = "no-cache";
		Response.Headers.Connection = "keep-alive";
		Response.Headers.AccessControlAllowOrigin = "*";
		Response.Headers.AccessControlAllowHeaders = "Cache-Control";

		var clientId = Guid.NewGuid().ToString("N")[..8];
		_logger.LogInformation("SSE client {ClientId} connected to NotificationsApi", clientId);

		try
		{
			// Send initial connection confirmation
			await WriteEventAsync("connected", new { clientId, timestamp = DateTime.UtcNow }, cancellationToken);

			// Subscribe to Redis pub/sub for notifications from Worker
			var subscriber = _redisDatabase.Multiplexer.GetSubscriber();
			var channel = await subscriber.SubscribeAsync(RedisChannel.Literal("sse:notifications"));
			
			var tcs = new TaskCompletionSource<bool>();
			cancellationToken.Register(() => tcs.TrySetCanceled());

			channel.OnMessage(async message =>
			{
				try
				{
					var notification = JsonSerializer.Deserialize<StudentNotificationEvent>(message!.Message.ToString(), JsonSerializerOptions.Web);
					if (notification != null)
					{
						var eventData = new
						{
							eventType = notification.EventType,
							routingKey = notification.RoutingKey,
							student = notification.Student,
							timestamp = notification.Timestamp,
							userId = notification.UserId
						};

						await WriteEventAsync(notification.EventType, eventData, cancellationToken);
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error processing SSE notification from Redis");
				}
			});

			// Wait until cancellation is requested
			try
			{
				await tcs.Task;
			}
			finally
			{
				await channel.UnsubscribeAsync();
			}
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("SSE client {ClientId} disconnected from NotificationsApi", clientId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in SSE stream for client {ClientId}", clientId);
		}

		return new EmptyResult();
	}

	private async Task WriteEventAsync(string eventType, object data, CancellationToken cancellationToken)
	{
		var json = JsonSerializer.Serialize(data, JsonSerializerOptions.Web);
		var eventMessage = $"event: {eventType}\ndata: {json}\n\n";
		var bytes = Encoding.UTF8.GetBytes(eventMessage);
		
		await Response.Body.WriteAsync(bytes, cancellationToken);
		await Response.Body.FlushAsync(cancellationToken);
	}
}