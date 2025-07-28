using TheBackgroundExperience.Domain.Entities;

namespace TheBackgroundExperience.Domain.Events;

public class CacheStudentEvent : BaseEvent
{
	public Student Student { get; }

	public CacheStudentEvent(Student student)
	{
		Student = student;
	}
}