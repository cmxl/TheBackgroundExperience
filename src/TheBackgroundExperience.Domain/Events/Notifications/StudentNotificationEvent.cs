using TheBackgroundExperience.Domain.Entities;

namespace TheBackgroundExperience.Domain.Events.Notifications;

public class StudentNotificationEvent
{
	public string EventType { get; }
	public string RoutingKey { get; }
	public Student Student { get; }
	public DateTime Timestamp { get; }
	public string? UserId { get; }

	public StudentNotificationEvent(
		string eventType,
		string routingKey,
		Student student,
		string? userId = null)
	{
		EventType = eventType;
		RoutingKey = routingKey;
		Student = student;
		Timestamp = DateTime.UtcNow;
		UserId = userId;
	}
}