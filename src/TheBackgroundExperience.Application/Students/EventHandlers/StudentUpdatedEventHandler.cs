using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheBackgroundExperience.Application.Common.Interfaces;
using TheBackgroundExperience.Application.Configuration;
using TheBackgroundExperience.Domain.Common.Notifications;
using TheBackgroundExperience.Domain.Events;
using TheBackgroundExperience.Domain.Events.Notifications;

namespace TheBackgroundExperience.Application.Students.EventHandlers;

public class StudentUpdatedEventHandler : INotificationHandler<StudentUpdatedEvent>
{
	private readonly ILogger<StudentUpdatedEventHandler> _logger;
	private readonly IQueueManager _queueManager;
	private readonly INotificationService _notificationService;
	private readonly RabbitMqConfig _config;

	public StudentUpdatedEventHandler(
		ILogger<StudentUpdatedEventHandler> logger,
		IQueueManager queueManager,
		INotificationService notificationService,
		IOptions<RabbitMqConfig> options)
	{
		_logger = logger;
		_queueManager = queueManager;
		_notificationService = notificationService;
		_config = options.Value;
	}

	public async ValueTask Handle(StudentUpdatedEvent notification, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Student updated: {@Student}", notification.Student);
		
		// Publish to worker queue (existing functionality)
		await _queueManager.PublishAsync(notification.Student, _config.QueueName, cancellationToken);
		
		// Publish real-time notification
		var studentNotification = new StudentNotificationEvent(
			"StudentUpdated",
			NotificationTopics.Student.Updated,
			notification.Student);
		
		await _notificationService.PublishStudentNotificationAsync(studentNotification, cancellationToken);
	}
}