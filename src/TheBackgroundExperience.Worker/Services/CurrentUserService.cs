using TheBackgroundExperience.Application.Common;

namespace TheBackgroundExperience.Worker.Services;

public class CurrentUserService : ICurrentUserService
{
	public string? UserId { get; set; } = nameof(TheBackgroundExperience.Worker);
}