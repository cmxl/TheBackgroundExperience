using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheBackgroundExperience.Application.Common.Interfaces;
using TheBackgroundExperience.Application.Configuration;
using TheBackgroundExperience.Domain.Events;

namespace TheBackgroundExperience.Application.Students.EventHandlers;

public class StudentUpdatedEventHandler : INotificationHandler<StudentUpdatedEvent>
{
	private readonly ILogger<StudentUpdatedEventHandler> _logger;
	private readonly IQueueManager _queueManager;
	private readonly RabbitMqConfig _config;

	public StudentUpdatedEventHandler(
		ILogger<StudentUpdatedEventHandler> logger,
		IQueueManager queueManager,
		IOptions<RabbitMqConfig> options)
	{
		_logger = logger;
		_queueManager = queueManager;
		_config = options.Value;
	}

	public async ValueTask Handle(StudentUpdatedEvent notification, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Student updated: {@Student}", notification.Student);
		await _queueManager.PublishAsync(notification.Student, _config.QueueName, cancellationToken);
	}
}