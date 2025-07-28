using Mediator;
using Microsoft.EntityFrameworkCore;
using TheBackgroundExperience.Application.Common.Exceptions;
using TheBackgroundExperience.Application.Common.Interfaces;
using TheBackgroundExperience.Domain.Common;
using TheBackgroundExperience.Domain.Common.Caching;
using TheBackgroundExperience.Domain.Entities;

namespace TheBackgroundExperience.Application.Students.Queries;

public class GetStudentByIdQuery : ICachableRequest<Student>
{
	public GetStudentByIdQuery(Guid id)
	{
		Id = id;
	}

	public Guid Id { get; }
	public string CacheKey => StudentCacheKeys.GetCacheKey(Id);
	public bool BypassCache { get; set; }
}

public class GetStudentByIdQueryHandler : IRequestHandler<GetStudentByIdQuery, Student>
{
	private readonly IApplicationDbContext _context;

	public GetStudentByIdQueryHandler(IApplicationDbContext context)
	{
		_context = context;
	}

	public async ValueTask<Student> Handle(GetStudentByIdQuery request, CancellationToken cancellationToken)
	{
		var student = await _context.Students
		                            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

		if (student == null) 
			throw new NotFoundException(nameof(Student), request.Id);

		return student;
	}
}