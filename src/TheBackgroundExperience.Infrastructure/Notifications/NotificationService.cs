using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TheBackgroundExperience.Application.Common.Interfaces;
using TheBackgroundExperience.Infrastructure.Messaging;
using TheBackgroundExperience.Application.Configuration;
using TheBackgroundExperience.Domain.Common.Notifications;
using TheBackgroundExperience.Domain.Events.Notifications;

namespace TheBackgroundExperience.Infrastructure.Notifications;

public class NotificationService : INotificationService
{
	private readonly ILogger<NotificationService> _logger;
	private readonly IRabbitMqConnectionPool _connectionPool;
	private readonly RabbitMqConfig _config;

	public NotificationService(
		ILogger<NotificationService> logger,
		IRabbitMqConnectionPool connectionPool,
		IOptions<RabbitMqConfig> options)
	{
		_logger = logger;
		_connectionPool = connectionPool;
		_config = options.Value;
	}

	public async Task PublishStudentNotificationAsync(StudentNotificationEvent notification, CancellationToken cancellationToken = default)
	{
		try
		{
			await _connectionPool.ExecuteWithChannelAsync(async (channel, ct) =>
			{
				// Declare the notifications exchange as topic
				await channel.ExchangeDeclareAsync(
					NotificationTopics.ExchangeName, 
					ExchangeType.Topic, 
					durable: true,
					cancellationToken: ct);

				var properties = new BasicProperties
				{
					Persistent = true,
					ContentType = "application/json",
					MessageId = Guid.NewGuid().ToString(),
					Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
					Headers = new Dictionary<string, object?>
					{
						["eventType"] = notification.EventType,
						["routingKey"] = notification.RoutingKey
					}
				};

				var messageBody = JsonSerializer.Serialize(notification, JsonSerializerOptions.Web);
				var body = Encoding.UTF8.GetBytes(messageBody);

				await channel.BasicPublishAsync(
					NotificationTopics.ExchangeName,
					notification.RoutingKey,
					mandatory: false,
					properties,
					body,
					ct);

				_logger.LogDebug("Published notification: {EventType} for Student {StudentId}", 
					notification.EventType, notification.Student.Id);
			}, cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to publish notification: {EventType} for Student {StudentId}",
				notification.EventType, notification.Student.Id);
			throw;
		}
	}
}