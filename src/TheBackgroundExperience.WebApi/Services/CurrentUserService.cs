using TheBackgroundExperience.Application.Common;

namespace TheBackgroundExperience.WebApi.Services;

public class CurrentUserService : ICurrentUserService
{
	public string? UserId { get; set; } = nameof(TheBackgroundExperience.WebApi);
}