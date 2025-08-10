using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TheBackgroundExperience.Application.Common.Interfaces;
using TheBackgroundExperience.Infrastructure.Messaging;
using TheBackgroundExperience.Application.Configuration;

namespace TheBackgroundExperience.Infrastructure.Queues;

public class QueueManager : IQueueManager
{
	private readonly RabbitMqConfig _config;
	private readonly IRabbitMqConnectionPool _connectionPool;
	private readonly ILogger<QueueManager> _logger;

	public QueueManager(
		IRabbitMqConnectionPool connectionPool,
		IOptions<RabbitMqConfig> options,
		ILogger<QueueManager> logger)
	{
		_connectionPool = connectionPool;
		_config = options.Value;
		_logger = logger;
	}

	public async Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default) where T : class
	{
		try
		{
			await _connectionPool.ExecuteWithChannelAsync(async (channel, ct) =>
			{
				await channel.QueueDeclareAsync(_config.QueueName, _config.Durable, _config.Exclusive, _config.AutoDelete, cancellationToken: ct);

				var props = new BasicProperties
				{
					Persistent = true,
					ContentType = "application/json",
					MessageId = Guid.NewGuid().ToString(),
					Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
				};

				var json = JsonSerializer.Serialize(message, JsonSerializerOptions.Web);
				var body = Encoding.UTF8.GetBytes(json);
				await channel.BasicPublishAsync(string.Empty, channel.CurrentQueue!, true, props, body, ct);

				_logger.LogDebug("Published message to queue {QueueName}: {MessageType}", queueName, typeof(T).Name);
			}, cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to publish message to queue {QueueName}: {MessageType}", queueName, typeof(T).Name);
			throw;
		}
	}
}