using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TheBackgroundExperience.Application.Common.Interfaces;
using TheBackgroundExperience.Application.Configuration;
using TheBackgroundExperience.Domain.Common.Notifications;
using TheBackgroundExperience.Domain.Events.Notifications;

namespace TheBackgroundExperience.Infrastructure.Notifications;

public class NotificationService : INotificationService
{
	private readonly ILogger<NotificationService> _logger;
	private readonly IConnectionFactory _connectionFactory;
	private readonly RabbitMqConfig _config;

	public NotificationService(
		ILogger<NotificationService> logger,
		IConnectionFactory connectionFactory,
		IOptions<RabbitMqConfig> options)
	{
		_logger = logger;
		_connectionFactory = connectionFactory;
		_config = options.Value;
	}

	public async Task PublishStudentNotificationAsync(StudentNotificationEvent notification, CancellationToken cancellationToken = default)
	{
		try
		{
			await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
			await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

			// Declare the notifications exchange as topic
			await channel.ExchangeDeclareAsync(
				NotificationTopics.ExchangeName, 
				ExchangeType.Topic, 
				durable: true,
				cancellationToken: cancellationToken);

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
				cancellationToken);

			_logger.LogInformation("Published notification: {EventType} for Student {StudentId}", 
				notification.EventType, notification.Student.Id);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to publish notification: {EventType} for Student {StudentId}",
				notification.EventType, notification.Student.Id);
			throw;
		}
	}
}