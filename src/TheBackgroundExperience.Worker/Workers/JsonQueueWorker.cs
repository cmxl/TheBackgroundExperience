using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TheBackgroundExperience.Application.Configuration;

namespace TheBackgroundExperience.Worker.Workers;

public abstract class JsonQueueWorker<T> : QueueWorkerBase
{
	protected JsonQueueWorker(
		ILogger<JsonQueueWorker<T>> logger,
		IOptions<RabbitMqConfig> options,
		IConnectionFactory factory)
		: base(logger, options, factory)
	{
	}

	protected sealed override async Task ProcessMessage(string message, CancellationToken cancellationToken)
	{
		var jsonMessage = System.Text.Json.JsonSerializer.Deserialize<T>(message, JsonSerializerOptions.Web);
		if (jsonMessage == null)
		{
			throw new InvalidOperationException("Failed to deserialize message");
		}

		await ProcessMessageInternal(jsonMessage, cancellationToken);
	}
	
	protected abstract Task ProcessMessageInternal(T message, CancellationToken cancellationToken);
}