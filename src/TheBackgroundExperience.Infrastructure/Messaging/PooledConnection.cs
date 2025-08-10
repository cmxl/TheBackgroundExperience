using System.Collections.Concurrent;
using RabbitMQ.Client;

namespace TheBackgroundExperience.Infrastructure.Messaging;

public class PooledConnection : IAsyncDisposable
{
	public string Id { get; }
	public IConnection Connection { get; }
	public DateTime CreatedAt { get; }
	public DateTime LastUsed { get; private set; }
	public bool IsActive { get; private set; }
	public bool IsHealthy => Connection.IsOpen && DateTime.UtcNow - LastUsed < TimeSpan.FromMinutes(30);
	public int ActiveChannelCount => _activeChannels.Count;
	public bool HasActiveChannels => _activeChannels.Count > 0;

	private readonly int _maxChannels;
	private readonly ConcurrentQueue<IChannel> _availableChannels;
	private readonly ConcurrentDictionary<int, IChannel> _activeChannels;
	private readonly SemaphoreSlim _channelSemaphore;
	private volatile bool _disposed;

	public PooledConnection(IConnection connection, int maxChannels = 20)
	{
		Id = Guid.NewGuid().ToString("N")[..8];
		Connection = connection;
		CreatedAt = DateTime.UtcNow;
		LastUsed = DateTime.UtcNow;
		_maxChannels = maxChannels;

		_availableChannels = new ConcurrentQueue<IChannel>();
		_activeChannels = new ConcurrentDictionary<int, IChannel>();
		_channelSemaphore = new SemaphoreSlim(maxChannels, maxChannels);
	}

	public void MarkAsActive()
	{
		IsActive = true;
		LastUsed = DateTime.UtcNow;
	}

	public void MarkAsAvailable()
	{
		IsActive = false;
		LastUsed = DateTime.UtcNow;
	}

	public async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default)
	{
		ThrowIfDisposed();

		var acquired = await _channelSemaphore.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
		if (!acquired)
		{
			throw new TimeoutException("Failed to acquire channel within timeout");
		}

		try
		{
			// Try to get an available channel from the pool
			while (_availableChannels.TryDequeue(out var channel))
			{
				if (channel.IsOpen)
				{
					_activeChannels.TryAdd(channel.ChannelNumber, channel);
					LastUsed = DateTime.UtcNow;
					return channel;
				}
				else
				{
					// Channel is closed, dispose it
					try
					{
						channel.Dispose();
					}
					catch { /* Ignore disposal errors */ }
				}
			}

			// Create new channel if none available
			var newChannel = await Connection.CreateChannelAsync(cancellationToken: cancellationToken);
			_activeChannels.TryAdd(newChannel.ChannelNumber, newChannel);
			LastUsed = DateTime.UtcNow;
			return newChannel;
		}
		catch
		{
			_channelSemaphore.Release();
			throw;
		}
	}

	public Task ReturnChannelAsync(IChannel channel, CancellationToken cancellationToken = default)
	{
		ThrowIfDisposed();

		if (_activeChannels.TryRemove(channel.ChannelNumber, out _))
		{
			if (channel.IsOpen)
			{
				_availableChannels.Enqueue(channel);
			}
			else
			{
				try
				{
					channel.Dispose();
				}
				catch { /* Ignore disposal errors */ }
			}

			_channelSemaphore.Release();
			LastUsed = DateTime.UtcNow;
		}

		return Task.CompletedTask;
	}

	public bool OwnsChannel(IChannel channel)
	{
		return _activeChannels.ContainsKey(channel.ChannelNumber) ||
		       _availableChannels.ToArray().Any(c => c.ChannelNumber == channel.ChannelNumber);
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(PooledConnection));
	}

	public async ValueTask DisposeAsync()
	{
		if (_disposed)
			return;

		_disposed = true;

		// Close all active channels
		foreach (var kvp in _activeChannels)
		{
			try
			{
				if (kvp.Value.IsOpen)
				{
					await kvp.Value.CloseAsync();
				}
				kvp.Value.Dispose();
			}
			catch { /* Ignore disposal errors */ }
		}

		// Close all available channels
		while (_availableChannels.TryDequeue(out var channel))
		{
			try
			{
				if (channel.IsOpen)
				{
					await channel.CloseAsync();
				}
				channel.Dispose();
			}
			catch { /* Ignore disposal errors */ }
		}

		// Close the connection
		try
		{
			if (Connection.IsOpen)
			{
				await Connection.CloseAsync();
			}
			Connection.Dispose();
		}
		catch { /* Ignore disposal errors */ }

		_channelSemaphore.Dispose();
	}
}