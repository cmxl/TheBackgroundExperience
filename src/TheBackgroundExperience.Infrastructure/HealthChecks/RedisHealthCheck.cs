using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace TheBackgroundExperience.Infrastructure.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
	private readonly IConnectionMultiplexer _connectionMultiplexer;
	private readonly ILogger<RedisHealthCheck> _logger;

	public RedisHealthCheck(
		IConnectionMultiplexer connectionMultiplexer,
		ILogger<RedisHealthCheck> logger)
	{
		_connectionMultiplexer = connectionMultiplexer;
		_logger = logger;
	}

	public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
	{
		try
		{
			var database = _connectionMultiplexer.GetDatabase();
			
			// Test basic connectivity with a simple ping operation
			var pingResult = await database.PingAsync();
			
			// Test read/write operations
			var testKey = $"health-check-{Guid.NewGuid():N}";
			var testValue = "health-check-value";
			
			await database.StringSetAsync(testKey, testValue, TimeSpan.FromSeconds(10));
			var retrievedValue = await database.StringGetAsync(testKey);
			await database.KeyDeleteAsync(testKey);

			if (retrievedValue != testValue)
			{
				throw new InvalidOperationException("Redis read/write test failed - values did not match");
			}

			var data = new Dictionary<string, object>
			{
				["ping_time_ms"] = pingResult.TotalMilliseconds,
				["endpoint"] = _connectionMultiplexer.Configuration,
				["is_connected"] = _connectionMultiplexer.IsConnected,
				["active_connections"] = _connectionMultiplexer.GetCounters().Interactive.TotalOutstanding
			};

			_logger.LogDebug("Redis health check passed - Ping: {PingMs}ms", pingResult.TotalMilliseconds);
			return HealthCheckResult.Healthy("Redis is healthy", data);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Redis health check failed");
			return HealthCheckResult.Unhealthy("Redis is unhealthy", ex);
		}
	}
}