using TheBackgroundExperience.Application.Common;

namespace TheBackgroundExperience.Infrastructure.Services;

public class DateTimeService : IDateTime
{
	public DateTimeOffset Now => DateTimeOffset.UtcNow;
}