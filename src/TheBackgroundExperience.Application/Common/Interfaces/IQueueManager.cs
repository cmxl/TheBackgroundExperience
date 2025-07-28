namespace TheBackgroundExperience.Application.Common.Interfaces;

public interface IQueueManager
{
	Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default) where T : class;
}