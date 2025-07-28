namespace TheBackgroundExperience.Domain.Common.Caching;

public static class StudentCacheKeys
{
	public static string GetCacheKey(Guid id) => $"Student_{id}";
}