using TheBackgroundExperience.Domain.Events.Notifications;

namespace TheBackgroundExperience.Application.Common.Interfaces;

public interface INotificationService
{
	Task PublishStudentNotificationAsync(StudentNotificationEvent notification, CancellationToken cancellationToken = default);
}