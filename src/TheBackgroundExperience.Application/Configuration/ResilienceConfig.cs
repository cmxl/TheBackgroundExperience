namespace TheBackgroundExperience.Application.Configuration;

public class ResilienceConfig
{
	public const string SectionName = "Resilience";

	public CircuitBreakerConfig RabbitMQ { get; init; } = new();
	public CircuitBreakerConfig Redis { get; init; } = new();
	public CircuitBreakerConfig Database { get; init; } = new();
}

public class CircuitBreakerConfig
{
	public TimeSpan DurationOfBreak { get; init; } = TimeSpan.FromSeconds(30);
	public int SamplingDuration { get; init; } = 60;
	public int MinimumThroughput { get; init; } = 10;
	public double FailureRatio { get; init; } = 0.5;
	public int MaxRetryAttempts { get; init; } = 3;
	public TimeSpan BaseDelay { get; init; } = TimeSpan.FromSeconds(1);
	public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);
	public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}