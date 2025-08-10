using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TheBackgroundExperience.Application.Configuration;

namespace TheBackgroundExperience.Infrastructure.HealthChecks;

public class RabbitMqHealthCheck : IHealthCheck
{
	private readonly IConnectionFactory _connectionFactory;
	private readonly RabbitMqConfig _config;
	private readonly ILogger<RabbitMqHealthCheck> _logger;

	public RabbitMqHealthCheck(
		IConnectionFactory connectionFactory,
		IOptions<RabbitMqConfig> config,
		ILogger<RabbitMqHealthCheck> logger)
	{
		_connectionFactory = connectionFactory;
		_config = config.Value;
		_logger = logger;
	}

	public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
	{
		try
		{
			await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
			await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

			// Verify we can declare a queue (lightweight operation)
			var queueName = $"health-check-{Guid.NewGuid():N}";
			await channel.QueueDeclareAsync(queueName, durable: false, exclusive: true, autoDelete: true, cancellationToken: cancellationToken);
			await channel.QueueDeleteAsync(queueName, cancellationToken: cancellationToken);

			var data = new Dictionary<string, object>
			{
				["hostname"] = _config.HostName,
				["port"] = _config.Port,
				["username"] = _config.UserName,
				["queue"] = _config.QueueName
			};

			_logger.LogDebug("RabbitMQ health check passed");
			return HealthCheckResult.Healthy("RabbitMQ is healthy", data);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "RabbitMQ health check failed");
			return HealthCheckResult.Unhealthy("RabbitMQ is unhealthy", ex);
		}
	}
}