using Mediator;
using TheBackgroundExperience.Application.Common.Interfaces;
using TheBackgroundExperience.Domain.Entities;
using TheBackgroundExperience.Domain.Events;

namespace TheBackgroundExperience.Application.Students.Commands;

public class UpdateStudentCommand : IRequest<Student>
{
	public Student Student { get; }

	public UpdateStudentCommand(
		Student student)
	{
		Student = student;
	}
}

public class UpdateStudentCommandHandler : IRequestHandler<UpdateStudentCommand, Student>
{
	private readonly IApplicationDbContext _context;

	public UpdateStudentCommandHandler(IApplicationDbContext context)
	{
		_context = context;
	}

	public async ValueTask<Student> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
	{
		var student = request.Student;

		_context.Students.Update(student);
		student.AddDomainEvent(new StudentUpdatedEvent(student));
		student.AddDomainEvent(new CacheStudentEvent(student));
		await _context.SaveChangesAsync(cancellationToken);

		return student;
	}
}