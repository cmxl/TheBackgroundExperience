using TheBackgroundExperience.Domain.Entities;

namespace TheBackgroundExperience.Domain.Events;

public class StudentCreatedEvent : BaseEvent
{
	public Student Student { get; }

	public StudentCreatedEvent(Student student)
	{
		Student = student;
	}
}