using System.Diagnostics;
using Mediator;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TheBackgroundExperience.Infrastructure.Common;

namespace TheBackgroundExperience.Infrastructure.Persistence.Interceptors;

public class DispatchEventsInterceptor : SaveChangesInterceptor
{
	private readonly IMediator _mediator;

	public DispatchEventsInterceptor(
		IMediator mediator)
	{
		_mediator = mediator;
	}
	
	public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = new CancellationToken())
	{
		Debug.Assert(eventData.Context != null, "eventData.Context != null");
		return await base.SavingChangesAsync(eventData, result, cancellationToken);
	}

	public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = new CancellationToken())
	{
		Debug.Assert(eventData.Context != null, "eventData.Context != null");
		var changes = await base.SavedChangesAsync(eventData, result, cancellationToken);
		// Dispatch domain events after saving changes => outbox pattern
		await _mediator.DispatchDomainEvents(eventData.Context);
		return changes;
	}
}