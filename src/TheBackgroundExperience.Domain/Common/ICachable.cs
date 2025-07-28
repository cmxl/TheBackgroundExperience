namespace TheBackgroundExperience.Domain.Common;

public interface ICachable
{
	string CacheKey { get; }
	bool BypassCache { get; }
}