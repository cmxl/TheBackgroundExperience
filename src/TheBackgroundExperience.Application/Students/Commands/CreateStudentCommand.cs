using Mediator;
using TheBackgroundExperience.Application.Common.Interfaces;
using TheBackgroundExperience.Domain.Entities;
using TheBackgroundExperience.Domain.Events;

namespace TheBackgroundExperience.Application.Students.Commands;

public class CreateStudentCommand : IRequest<Student>
{
	public Student Student { get; }

	public CreateStudentCommand(
		Student student)
	{
		Student = student;
	}
}

public class CreateStudentCommandHandler : IRequestHandler<CreateStudentCommand, Student>
{
	private readonly IApplicationDbContext _context;

	public CreateStudentCommandHandler(IApplicationDbContext context)
	{
		_context = context;
	}

	public async ValueTask<Student> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
	{
		var student = request.Student;

		_context.Students.Add(student);
		student.AddDomainEvent(new StudentCreatedEvent(student));
		student.AddDomainEvent(new CacheStudentEvent(student));
		await _context.SaveChangesAsync(cancellationToken);

		return student;
	}
}