using Mediator;
using Microsoft.Extensions.Logging;
using TheBackgroundExperience.Application.Common.Interfaces;
using TheBackgroundExperience.Domain.Common.Notifications;
using TheBackgroundExperience.Domain.Events;
using TheBackgroundExperience.Domain.Events.Notifications;
using ZiggyCreatures.Caching.Fusion;

namespace TheBackgroundExperience.Application.Students.EventHandlers;

internal class CacheStudentEventHandler : INotificationHandler<CacheStudentEvent>
{
	private readonly ILogger<CacheStudentEventHandler> _logger;
	private readonly IFusionCache _cache;
	private readonly INotificationService _notificationService;

	public CacheStudentEventHandler(
		ILogger<CacheStudentEventHandler> logger,
		IFusionCache cache,
		INotificationService notificationService)
	{
		_logger = logger;
		_cache = cache;
		_notificationService = notificationService;
	}
	
	public async ValueTask Handle(CacheStudentEvent notification, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Caching student: {@Student}", notification.Student);
		await _cache.SetAsync(notification.Student.CacheKey, notification.Student, TimeSpan.FromMinutes(15), cancellationToken);
		
		// Publish cache update notification
		var studentNotification = new StudentNotificationEvent(
			"StudentCached",
			NotificationTopics.Student.Cached,
			notification.Student);
		
		await _notificationService.PublishStudentNotificationAsync(studentNotification, cancellationToken);
	}
}