using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using TheBackgroundExperience.Application.Configuration;
using TheBackgroundExperience.Infrastructure.Resilience;

namespace TheBackgroundExperience.Infrastructure.Messaging;

public class RabbitMqConnectionPool : IRabbitMqConnectionPool
{
	private readonly ILogger<RabbitMqConnectionPool> _logger;
	private readonly IConnectionFactory _connectionFactory;
	private readonly RabbitMqConnectionPoolConfig _config;
	private readonly ResiliencePipeline _resiliencePipeline;
	private readonly ConcurrentQueue<PooledConnection> _availableConnections;
	private readonly ConcurrentDictionary<string, PooledConnection> _activeConnections;
	private readonly SemaphoreSlim _connectionSemaphore;
	private readonly Timer? _healthCheckTimer;
	private volatile bool _disposed;

	public RabbitMqConnectionPool(
		ILogger<RabbitMqConnectionPool> logger,
		IConnectionFactory connectionFactory,
		IOptions<RabbitMqConnectionPoolConfig> config,
		ResiliencePipelineFactory resiliencePipelineFactory)
	{
		_logger = logger;
		_connectionFactory = connectionFactory;
		_config = config.Value;
		_resiliencePipeline = resiliencePipelineFactory.GetPipeline("rabbitmq");
		
		_availableConnections = new ConcurrentQueue<PooledConnection>();
		_activeConnections = new ConcurrentDictionary<string, PooledConnection>();
		_connectionSemaphore = new SemaphoreSlim(_config.MaxConnections, _config.MaxConnections);

		if (_config.EnableHealthChecks)
		{
			_healthCheckTimer = new Timer(PerformHealthCheck, null, 
				_config.HealthCheckInterval, _config.HealthCheckInterval);
		}

		_logger.LogInformation("RabbitMQ connection pool initialized with max connections: {MaxConnections}", 
			_config.MaxConnections);
	}

	public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
	{
		ThrowIfDisposed();

		var acquired = await _connectionSemaphore.WaitAsync(_config.ConnectionTimeout, cancellationToken);
		if (!acquired)
		{
			throw new TimeoutException($"Failed to acquire connection within {_config.ConnectionTimeout}");
		}

		try
		{
			// Try to get an available connection from the pool
			while (_availableConnections.TryDequeue(out var pooledConnection))
			{
				if (pooledConnection.Connection.IsOpen && pooledConnection.IsHealthy)
				{
					_activeConnections.TryAdd(pooledConnection.Id, pooledConnection);
					pooledConnection.MarkAsActive();
					_logger.LogDebug("Reused existing connection {ConnectionId}", pooledConnection.Id);
					return pooledConnection.Connection;
				}
				else
				{
					// Connection is not healthy, dispose it
					await DisposePooledConnectionAsync(pooledConnection);
				}
			}

			// Create new connection if none available
			var connection = await CreateNewConnectionAsync(cancellationToken);
			var newPooledConnection = new PooledConnection(connection, _config.MaxChannelsPerConnection);
			_activeConnections.TryAdd(newPooledConnection.Id, newPooledConnection);
			newPooledConnection.MarkAsActive();

			_logger.LogDebug("Created new connection {ConnectionId}", newPooledConnection.Id);
			return connection;
		}
		catch
		{
			_connectionSemaphore.Release();
			throw;
		}
	}

	public async Task ReturnConnectionAsync(IConnection connection, CancellationToken cancellationToken = default)
	{
		ThrowIfDisposed();

		var connectionId = FindConnectionId(connection);
		if (connectionId != null && _activeConnections.TryRemove(connectionId, out var pooledConnection))
		{
			pooledConnection.MarkAsAvailable();

			if (connection.IsOpen && pooledConnection.IsHealthy)
			{
				_availableConnections.Enqueue(pooledConnection);
				_logger.LogDebug("Returned connection {ConnectionId} to pool", connectionId);
			}
			else
			{
				await DisposePooledConnectionAsync(pooledConnection);
				_logger.LogDebug("Disposed unhealthy connection {ConnectionId}", connectionId);
			}

			_connectionSemaphore.Release();
		}
	}

	public async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default)
	{
		ThrowIfDisposed();

		// Get the least loaded connection from pool
		PooledConnection? selectedConnection = null;
		
		// Try to find an available connection first
		while (_availableConnections.TryDequeue(out var pooledConnection))
		{
			if (pooledConnection.Connection.IsOpen && pooledConnection.IsHealthy)
			{
				selectedConnection = pooledConnection;
				_activeConnections.TryAdd(pooledConnection.Id, pooledConnection);
				pooledConnection.MarkAsActive();
				break;
			}
			else
			{
				await DisposePooledConnectionAsync(pooledConnection);
			}
		}

		// If no available connection, try to get from active connections (least loaded)
		if (selectedConnection == null)
		{
			foreach (var kvp in _activeConnections)
			{
				if (kvp.Value.Connection.IsOpen && kvp.Value.IsHealthy)
				{
					selectedConnection = kvp.Value;
					break;
				}
			}
		}

		// If still no connection, create a new one if possible
		if (selectedConnection == null)
		{
			var acquired = await _connectionSemaphore.WaitAsync(_config.ConnectionTimeout, cancellationToken);
			if (!acquired)
			{
				throw new TimeoutException($"Failed to acquire connection within {_config.ConnectionTimeout}");
			}

			try
			{
				var connection = await CreateNewConnectionAsync(cancellationToken);
				selectedConnection = new PooledConnection(connection, _config.MaxChannelsPerConnection);
				_activeConnections.TryAdd(selectedConnection.Id, selectedConnection);
				selectedConnection.MarkAsActive();
				_logger.LogDebug("Created new connection {ConnectionId} for channel", selectedConnection.Id);
			}
			catch
			{
				_connectionSemaphore.Release();
				throw;
			}
		}

		return await selectedConnection.GetChannelAsync(cancellationToken);
	}

	public async Task ReturnChannelAsync(IChannel channel, CancellationToken cancellationToken = default)
	{
		ThrowIfDisposed();

		// Find the connection that owns this channel
		PooledConnection? owningConnection = null;
		string? connectionId = null;

		foreach (var kvp in _activeConnections)
		{
			if (kvp.Value.OwnsChannel(channel))
			{
				owningConnection = kvp.Value;
				connectionId = kvp.Key;
				break;
			}
		}

		if (owningConnection != null && connectionId != null)
		{
			await owningConnection.ReturnChannelAsync(channel, cancellationToken);
			
			// Only return connection to available pool if it has no active channels
			// This prevents premature connection reuse while channels are still active
			if (owningConnection.Connection.IsOpen && owningConnection.IsHealthy && !owningConnection.HasActiveChannels)
			{
				// Remove from active and add to available pool for reuse
				if (_activeConnections.TryRemove(connectionId, out var removedConnection))
				{
					removedConnection.MarkAsAvailable();
					_availableConnections.Enqueue(removedConnection);
					_logger.LogDebug("Returned connection {ConnectionId} to available pool (no active channels)", connectionId);
				}
			}
			else
			{
				_logger.LogDebug("Connection {ConnectionId} kept active (has {ActiveChannels} active channels)", 
					connectionId, owningConnection.ActiveChannelCount);
			}
			return;
		}

		// Channel not managed by pool, dispose directly
		try
		{
			if (channel.IsOpen)
			{
				await channel.CloseAsync(cancellationToken);
			}
			channel.Dispose();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Error disposing unmanaged channel");
		}
	}

	public async Task<T> ExecuteWithChannelAsync<T>(Func<IChannel, CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
	{
		return await _resiliencePipeline.ExecuteAsync(async (ct) =>
		{
			var channel = await GetChannelAsync(ct);
			try
			{
				return await operation(channel, ct);
			}
			finally
			{
				await ReturnChannelAsync(channel, ct);
			}
		}, cancellationToken);
	}

	public async Task ExecuteWithChannelAsync(Func<IChannel, CancellationToken, Task> operation, CancellationToken cancellationToken = default)
	{
		await _resiliencePipeline.ExecuteAsync(async (ct) =>
		{
			var channel = await GetChannelAsync(ct);
			try
			{
				await operation(channel, ct);
			}
			finally
			{
				await ReturnChannelAsync(channel, ct);
			}
		}, cancellationToken);
	}

	private async Task<IConnection> CreateNewConnectionAsync(CancellationToken cancellationToken)
	{
		return await _resiliencePipeline.ExecuteAsync(async (ct) =>
		{
			var connection = await _connectionFactory.CreateConnectionAsync(ct);
			_logger.LogDebug("Successfully created new RabbitMQ connection");
			return connection;
		}, cancellationToken);
	}

	private string? FindConnectionId(IConnection connection)
	{
		foreach (var kvp in _activeConnections)
		{
			if (ReferenceEquals(kvp.Value.Connection, connection))
			{
				return kvp.Key;
			}
		}

		// Also check available connections
		var tempQueue = new ConcurrentQueue<PooledConnection>();
		string? foundId = null;

		while (_availableConnections.TryDequeue(out var pooledConnection))
		{
			if (ReferenceEquals(pooledConnection.Connection, connection))
			{
				foundId = pooledConnection.Id;
			}
			tempQueue.Enqueue(pooledConnection);
		}

		// Restore the queue
		while (tempQueue.TryDequeue(out var pooledConnection))
		{
			_availableConnections.Enqueue(pooledConnection);
		}

		return foundId;
	}


	private async Task DisposePooledConnectionAsync(PooledConnection pooledConnection)
	{
		try
		{
			await pooledConnection.DisposeAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error disposing pooled connection {ConnectionId}", pooledConnection.Id);
		}
	}

	private async void PerformHealthCheck(object? state)
	{
		if (_disposed)
			return;

		try
		{
			_logger.LogDebug("Performing health check on connection pool");

			var unhealthyConnections = new List<string>();

			// Check active connections
			foreach (var kvp in _activeConnections)
			{
				if (!kvp.Value.Connection.IsOpen || !kvp.Value.IsHealthy)
				{
					unhealthyConnections.Add(kvp.Key);
				}
			}

			// Remove unhealthy active connections
			foreach (var connectionId in unhealthyConnections)
			{
				if (_activeConnections.TryRemove(connectionId, out var pooledConnection))
				{
					await DisposePooledConnectionAsync(pooledConnection);
					_connectionSemaphore.Release();
					_logger.LogWarning("Removed unhealthy active connection {ConnectionId}", connectionId);
				}
			}

			// Check available connections
			var healthyConnections = new ConcurrentQueue<PooledConnection>();
			while (_availableConnections.TryDequeue(out var pooledConnection))
			{
				if (pooledConnection.Connection.IsOpen && pooledConnection.IsHealthy)
				{
					healthyConnections.Enqueue(pooledConnection);
				}
				else
				{
					await DisposePooledConnectionAsync(pooledConnection);
					_logger.LogWarning("Removed unhealthy available connection {ConnectionId}", pooledConnection.Id);
				}
			}

			// Restore healthy connections
			while (healthyConnections.TryDequeue(out var pooledConnection))
			{
				_availableConnections.Enqueue(pooledConnection);
			}

			_logger.LogDebug("Health check completed. Active: {Active}, Available: {Available}", 
				_activeConnections.Count, _availableConnections.Count);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during connection pool health check");
		}
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(RabbitMqConnectionPool));
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;

		_healthCheckTimer?.Dispose();

		// Dispose all active connections
		foreach (var kvp in _activeConnections)
		{
			try
			{
				kvp.Value.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error disposing active connection {ConnectionId}", kvp.Key);
			}
		}

		// Dispose all available connections
		while (_availableConnections.TryDequeue(out var pooledConnection))
		{
			try
			{
				pooledConnection.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error disposing available connection {ConnectionId}", pooledConnection.Id);
			}
		}

		_connectionSemaphore.Dispose();

		_logger.LogInformation("RabbitMQ connection pool disposed");
	}
}