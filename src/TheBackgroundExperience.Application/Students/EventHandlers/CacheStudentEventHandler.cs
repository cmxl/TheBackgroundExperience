using Mediator;
using Microsoft.Extensions.Logging;
using TheBackgroundExperience.Domain.Events;
using ZiggyCreatures.Caching.Fusion;

namespace TheBackgroundExperience.Application.Students.EventHandlers;

internal class CacheStudentEventHandler : INotificationHandler<CacheStudentEvent>
{
	private readonly ILogger<CacheStudentEventHandler> _logger;
	private readonly IFusionCache _cache;

	public CacheStudentEventHandler(
		ILogger<CacheStudentEventHandler> logger,
		IFusionCache cache)
	{
		_logger = logger;
		_cache = cache;
	}
	
	public async ValueTask Handle(CacheStudentEvent notification, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Caching student: {@Student}", notification.Student);
		await _cache.SetAsync(notification.Student.CacheKey, notification.Student, TimeSpan.FromMinutes(15), cancellationToken);
	}
}