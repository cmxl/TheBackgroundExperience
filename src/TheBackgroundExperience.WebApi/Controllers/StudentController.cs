using Bogus;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using TheBackgroundExperience.Application.Students.Commands;
using TheBackgroundExperience.Application.Students.Queries;
using TheBackgroundExperience.Domain.Entities;

namespace TheBackgroundExperience.WebApi.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class StudentController : ControllerBase
{
	private readonly IMediator _mediator;

	private readonly Faker<Student> _faker = new Faker<Student>().RuleFor(x => x.Id, (_) => Guid.CreateVersion7())
	                                                             .RuleFor(x => x.FirstName, (faker) => faker.Name.FirstName())
	                                                             .RuleFor(x => x.LastName, (faker) => faker.Name.LastName());

	public StudentController(
		IMediator mediator)
	{
		_mediator = mediator;
	}

	[HttpPost]
	public async Task<ActionResult> Create(CancellationToken cancellationToken)
	{
		var student = _faker.Generate();
		var command = new CreateStudentCommand(student);
		await _mediator.Send(command, cancellationToken);
		return CreatedAtAction("GetById", routeValues: new { id = student.Id }, value: student);
	}

	[HttpPut("{id}")]
	public async Task<ActionResult> Update([FromRoute] Guid id, CancellationToken cancellationToken)
	{
		var fake = _faker.Generate();
		var query = new GetStudentByIdQuery(id);
		var student = await _mediator.Send(query, cancellationToken);
		
		student.FirstName = fake.FirstName;
		student.LastName = fake.LastName;
		
		var command = new UpdateStudentCommand(student);
		await _mediator.Send(command, cancellationToken);
		return Ok();
	}
	
	[HttpGet("{id}")]
	public async Task<ActionResult<Student>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
	{
		var student = new GetStudentByIdQuery(id);
		return await _mediator.Send(student, cancellationToken);
	}

}