using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using TheBackgroundExperience.Application.Configuration;
using TheBackgroundExperience.Domain.Common.Notifications;
using TheBackgroundExperience.Domain.Events.Notifications;
using TheBackgroundExperience.NotificationsApi.Hubs;

namespace TheBackgroundExperience.NotificationsApi.Services;

public class NotificationConsumerService : BackgroundService
{
	private readonly ILogger<NotificationConsumerService> _logger;
	private readonly IConnectionFactory _connectionFactory;
	private readonly IDatabase _redisDatabase;
	private readonly IHubContext<StudentNotificationHub> _hubContext;
	private readonly RabbitMqConfig _config;
	
	private IConnection? _connection;
	private IChannel? _channel;

	public NotificationConsumerService(
		ILogger<NotificationConsumerService> logger,
		IHubContext<StudentNotificationHub> hubContext,
		IDatabase redisDatabase,
		IServiceProvider serviceProvider)
	{
		_logger = logger;
		_hubContext = hubContext;
		_redisDatabase = redisDatabase;

		// Create RabbitMQ connection factory
		var configuration = serviceProvider.GetRequiredService<IConfiguration>();
		_config = configuration.GetSection(RabbitMqConfig.SectionName).Get<RabbitMqConfig>()!;
		
		_connectionFactory = new ConnectionFactory
		{
			HostName = _config.HostName,
			Port = _config.Port,
			UserName = _config.UserName,
			Password = _config.Password
		};
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			_connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
			_channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

			// Declare exchange and queue for notifications
			await _channel.ExchangeDeclareAsync(
				NotificationTopics.ExchangeName, 
				ExchangeType.Topic, 
				durable: true,
				cancellationToken: stoppingToken);

			var queueName = _config.QueueName;
			await _channel.QueueDeclareAsync(
				queueName, 
				durable: _config.Durable, 
				exclusive: _config.Exclusive, 
				autoDelete: _config.AutoDelete,
				cancellationToken: stoppingToken);

			// Bind to all student notification types
			await _channel.QueueBindAsync(
				queueName,
				NotificationTopics.ExchangeName,
				NotificationTopics.Routing.AllStudents,
				cancellationToken: stoppingToken);

			// Set up consumer
			var consumer = new AsyncEventingBasicConsumer(_channel);
			consumer.ReceivedAsync += OnNotificationReceived;

			await _channel.BasicConsumeAsync(
				queueName,
				autoAck: false,
				consumer: consumer,
				cancellationToken: stoppingToken);

			_logger.LogInformation("NotificationConsumerService started in NotificationsApi, listening for notifications");

			// Keep the service running
			while (!stoppingToken.IsCancellationRequested)
			{
				await Task.Delay(1000, stoppingToken);
			}
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("NotificationConsumerService is stopping");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in NotificationConsumerService");
			throw;
		}
		finally
		{
			await CleanupAsync();
		}
	}

	private async Task OnNotificationReceived(object sender, BasicDeliverEventArgs eventArgs)
	{
		try
		{
			var body = eventArgs.Body.ToArray();
			var message = Encoding.UTF8.GetString(body);
			
			var notification = JsonSerializer.Deserialize<StudentNotificationEvent>(message, JsonSerializerOptions.Web);
			
			if (notification != null)
			{
				// Broadcast via SignalR to all connected clients
				await BroadcastViaSignalR(notification);
				
				// Publish to Redis for SSE clients
				await BroadcastViaSSE(notification);
				
				_logger.LogDebug("Broadcasted notification: {EventType} for Student {StudentId}", 
					notification.EventType, notification.Student.Id);
			}

			// Acknowledge the message
			if (_channel != null)
			{
				await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing notification message");
			
			// Reject and requeue the message
			if (_channel != null)
			{
				await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
			}
		}
	}

	private async Task BroadcastViaSignalR(StudentNotificationEvent notification)
	{
		try
		{
			var eventData = new
			{
				eventType = notification.EventType,
				routingKey = notification.RoutingKey,
				student = notification.Student,
				timestamp = notification.Timestamp,
				userId = notification.UserId
			};

			// Send to all clients in Students group
			await _hubContext.Clients.Group("Students").SendAsync(notification.EventType, eventData);
			
			_logger.LogDebug("Sent SignalR notification: {EventType}", notification.EventType);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error broadcasting SignalR notification");
		}
	}

	private async Task BroadcastViaSSE(StudentNotificationEvent notification)
	{
		try
		{
			// Publish to Redis for SSE endpoints to consume
			var redisMessage = JsonSerializer.Serialize(notification, JsonSerializerOptions.Web);
			await _redisDatabase.PublishAsync(RedisChannel.Literal("sse:notifications"), redisMessage);
			
			_logger.LogDebug("Published SSE notification to Redis: {EventType}", notification.EventType);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error broadcasting SSE notification");
		}
	}

	private async Task CleanupAsync()
	{
		try
		{
			if (_channel != null)
			{
				await _channel.CloseAsync();
				_channel.Dispose();
			}
			
			if (_connection != null)
			{
				await _connection.CloseAsync();
				_connection.Dispose();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during NotificationConsumerService cleanup");
		}
	}

	public override void Dispose()
	{
		Task.Run(CleanupAsync).Wait(TimeSpan.FromSeconds(5));
		base.Dispose();
		GC.SuppressFinalize(this);
	}
}