using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TheBackgroundExperience.Application.Configuration;
using TheBackgroundExperience.Infrastructure.Messaging;

namespace TheBackgroundExperience.Worker.Workers;

public abstract class QueueWorkerBase : BackgroundService
{
	private readonly IRabbitMqConnectionPool _connectionPool;
	private readonly ILogger _logger;
	private readonly RabbitMqConfig _config;
	private IChannel? _channel;
	private AsyncEventingBasicConsumer? _consumer;

	public QueueWorkerBase(
		ILogger logger,
		IOptions<RabbitMqConfig> options,
		IRabbitMqConnectionPool connectionPool)
	{
		_logger = logger;
		_connectionPool = connectionPool;
		_config = options.Value;
	}

	public override async Task StartAsync(CancellationToken cancellationToken)
	{
		_channel = await _connectionPool.GetChannelAsync(cancellationToken);
		await _channel.QueueDeclareAsync(_config.QueueName, _config.Durable, _config.Exclusive, _config.AutoDelete, cancellationToken: cancellationToken);
		await _channel.BasicQosAsync(0, _config.PrefetchCount, false, cancellationToken);

		_consumer = new AsyncEventingBasicConsumer(_channel);
		_consumer.ReceivedAsync += async (sender, ea) =>
		{
			var body = ea.Body.ToArray();
			var message = Encoding.UTF8.GetString(body);
			_logger.LogInformation(" [x] Received {Message}", message);

			await ProcessMessage(message, cancellationToken);

			_logger.LogInformation(" [x] Finished processing {Message}", message);

			var c = (sender as AsyncEventingBasicConsumer)!.Channel;
			await c.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
		};
		await base.StartAsync(cancellationToken);
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		if (_channel != null)
		{
			await _connectionPool.ReturnChannelAsync(_channel, cancellationToken);
			_channel = null;
		}

		_consumer = null;

		await base.StopAsync(cancellationToken);
	}


	protected override async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			if(_channel == null)
				throw new InvalidOperationException("Channel is not initialized. Ensure StartAsync has been called.");
			
			if(_consumer == null)
				throw new InvalidOperationException("Consumer is not initialized. Ensure StartAsync has been called.");
			
			try
			{
				if (_logger.IsEnabled(LogLevel.Information))
					_logger.LogTrace("Worker running at: {Time}", DateTimeOffset.Now);

				await _channel.BasicConsumeAsync(_config.QueueName, false, _consumer, cancellationToken: cancellationToken);
			}
			catch (Exception exc)
			{
				_logger.LogError(exc, "Unexpected error while executing worker");
				await Task.Delay(4000, cancellationToken);
			}
			finally
			{
				await Task.Delay(1000, cancellationToken);
			}
		}

	}

	protected abstract Task ProcessMessage(string message, CancellationToken cancellationToken);
}