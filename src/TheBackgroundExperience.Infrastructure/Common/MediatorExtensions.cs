using Mediator;
using Microsoft.EntityFrameworkCore;
using TheBackgroundExperience.Domain.Common;

namespace TheBackgroundExperience.Infrastructure.Common;

public static class MediatorExtensions
{
	public static async Task DispatchDomainEvents(this IMediator mediator, DbContext context)
	{
		var entities = context.ChangeTracker
		                      .Entries<BaseEntity>()
		                      .Where(e => e.Entity.DomainEvents.Any())
		                      .Select(e => e.Entity)
		                      .ToList();

		var domainEvents = entities
		                   .SelectMany(e => e.DomainEvents)
		                   .ToList();

		entities.ForEach(e => e.ClearDomainEvents());

		foreach (var domainEvent in domainEvents)
			await mediator.Publish(domainEvent);
	}
}