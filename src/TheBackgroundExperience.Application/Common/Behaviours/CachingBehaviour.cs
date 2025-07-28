using Mediator;
using Microsoft.Extensions.Logging;
using TheBackgroundExperience.Application.Common.Interfaces;
using TheBackgroundExperience.Domain.Common;
using ZiggyCreatures.Caching.Fusion;

namespace TheBackgroundExperience.Application.Common.Behaviours;

public class CachingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : ICachableRequest<TResponse>
{
	private readonly IFusionCache _cache;
	private readonly ILogger<TRequest> _logger;

	public CachingBehaviour(
		ILogger<TRequest> logger,
		IFusionCache cache)
	{
		_logger = logger;
		_cache = cache;
	}

	public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
	{
		if(message.BypassCache)
		{
			_logger.LogInformation("Bypassing cache for {@Message}", message);
			return await next(message, cancellationToken);
		}
		
		_logger.LogInformation("Trying to serve {@Message} from cache", message);
		try
		{
			return await _cache.GetOrSetAsync(message.CacheKey, async cts =>
			{
				_logger.LogInformation("Cache miss for {@CacheKey}, executing handler", message.CacheKey);
				var response = await next(message, cts);
				_logger.LogInformation("Handler executed, caching result for {CacheKey}", message.CacheKey);
				return response;
			}, token: cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error while trying to serve {@Message} from cache", message);
			throw;
		}
	}
}