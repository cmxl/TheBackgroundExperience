using Mediator;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TheBackgroundExperience.Application.Common.Interfaces;
using TheBackgroundExperience.Application.Configuration;
using TheBackgroundExperience.Application.Students.Queries;
using TheBackgroundExperience.Domain.Entities;
using TheBackgroundExperience.Domain.Events;

namespace TheBackgroundExperience.Worker.Workers;

public class StudentQueueWorker : JsonQueueWorker<Student>
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<JsonQueueWorker<Student>> _logger;

	public StudentQueueWorker(
		IServiceProvider serviceProvider,
		ILogger<StudentQueueWorker> logger, 
		IOptions<RabbitMqConfig> options, 
		IConnectionFactory factory) 
		: base(logger, options, factory)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	protected override async Task ProcessMessageInternal(Student message, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Processing {@Student}", message);
		
		// For demonstration purposes, we will just update the student in the database without changing any properties
		await using var scope = _serviceProvider.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
		var student = await mediator.Send(new GetStudentByIdQuery(message.Id) { BypassCache = true }, cancellationToken);
		student.Updates++;
		context.Students.Update(student);
		student.AddDomainEvent(new CacheStudentEvent(student));
		await context.SaveChangesAsync(cancellationToken);
	}
}