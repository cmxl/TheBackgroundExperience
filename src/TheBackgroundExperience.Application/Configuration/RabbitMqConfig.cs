namespace TheBackgroundExperience.Application.Configuration;

public class RabbitMqConfig
{
	public const string SectionName = "RabbitMQ";

	public required string QueueName { get; init; }
	public required string HostName { get; init; }
	public required string UserName { get; init; }
	public required string Password { get; init; }
	public int Port { get; init; }
	public bool Durable { get; init; }
	public bool AutoDelete { get; init; }
	public bool Exclusive { get; init; }
	public ushort PrefetchCount { get; init; }
}