using RabbitMQ.Client;

namespace TheBackgroundExperience.Infrastructure.Messaging;

public interface IRabbitMqConnectionPool : IDisposable
{
	Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
	Task ReturnConnectionAsync(IConnection connection, CancellationToken cancellationToken = default);
	Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default);
	Task ReturnChannelAsync(IChannel channel, CancellationToken cancellationToken = default);
	Task<T> ExecuteWithChannelAsync<T>(Func<IChannel, CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
	Task ExecuteWithChannelAsync(Func<IChannel, CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}