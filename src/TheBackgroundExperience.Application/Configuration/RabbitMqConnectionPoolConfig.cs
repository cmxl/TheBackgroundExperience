namespace TheBackgroundExperience.Application.Configuration;

public class RabbitMqConnectionPoolConfig
{
	public const string SectionName = "RabbitMQConnectionPool";

	public int MaxConnections { get; init; } = 5;
	public int MaxChannelsPerConnection { get; init; } = 10;
	public TimeSpan ConnectionTimeout { get; init; } = TimeSpan.FromSeconds(30);
	public TimeSpan ChannelTimeout { get; init; } = TimeSpan.FromSeconds(10);
	public TimeSpan HealthCheckInterval { get; init; } = TimeSpan.FromMinutes(2);
	public int MaxRetryAttempts { get; init; } = 3;
	public bool EnableHealthChecks { get; init; } = true;
	public TimeSpan ConnectionIdleTimeout { get; init; } = TimeSpan.FromMinutes(10);
}