using TheBackgroundExperience.Domain.Entities;

namespace TheBackgroundExperience.Domain.Events;

public class StudentUpdatedEvent : BaseEvent
{
	public Student Student { get; }

	public StudentUpdatedEvent(Student student)
	{
		Student = student;
	}
}