using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TheBackgroundExperience.Application.Common.Interfaces;
using TheBackgroundExperience.Application.Configuration;

namespace TheBackgroundExperience.Infrastructure.Queues;

public class QueueManager : IQueueManager
{
	private readonly RabbitMqConfig _config;
	private readonly IConnectionFactory _connectionFactory;

	public QueueManager(
		IConnectionFactory connectionFactory,
		IOptions<RabbitMqConfig> options)
	{
		_connectionFactory = connectionFactory;
		_config = options.Value;
	}

	public async Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default) where T : class
	{
		// the connection creation and channel creation are done here
		// need to have a look at the best practices for connection management
		// this could potentially lead to SNAT port exhaustion if not handled properly
		// and is possibly slow in high throughput scenarios
		await using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
		await using var channel = await conn.CreateChannelAsync(cancellationToken: cancellationToken);

		await channel.QueueDeclareAsync(_config.QueueName, _config.Durable, _config.Exclusive, _config.AutoDelete, cancellationToken: cancellationToken);

		var props = new BasicProperties();
		props.Persistent = true;
		props.ContentType = "application/json";

		var json = JsonSerializer.Serialize(message, JsonSerializerOptions.Web);
		var body = Encoding.UTF8.GetBytes(json);
		await channel.BasicPublishAsync(string.Empty, channel.CurrentQueue!, true, props, body, cancellationToken);
	}
}