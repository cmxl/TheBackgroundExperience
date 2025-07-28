using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheBackgroundExperience.Application.Common.Interfaces;
using TheBackgroundExperience.Application.Configuration;
using TheBackgroundExperience.Domain.Events;

namespace TheBackgroundExperience.Application.Students.EventHandlers;

public class StudentCreatedEventHandler : INotificationHandler<StudentCreatedEvent>
{
	private readonly ILogger<StudentCreatedEventHandler> _logger;
	private readonly IQueueManager _queueManager;
	private readonly RabbitMqConfig _config;

	public StudentCreatedEventHandler(
		ILogger<StudentCreatedEventHandler> logger,
		IQueueManager queueManager,
		IOptions<RabbitMqConfig> options)
	{
		_logger = logger;
		_queueManager = queueManager;
		_config = options.Value;
	}

	public async ValueTask Handle(StudentCreatedEvent notification, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Student created: {@Student}", notification.Student);
		await _queueManager.PublishAsync(notification.Student, _config.QueueName, cancellationToken);
	}
}