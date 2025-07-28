namespace TheBackgroundExperience.Application.Common;

public interface IDateTime
{
	DateTimeOffset Now { get; }
}