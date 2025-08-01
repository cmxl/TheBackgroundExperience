using System.ComponentModel.DataAnnotations.Schema;

namespace TheBackgroundExperience.Domain.Common;

public abstract class BaseEntity<T> : BaseEntity where T : struct
{
	public T Id { get; set; }
}

public abstract class BaseEntity
{
	private readonly List<BaseEvent> _domainEvents = new();

	[NotMapped] public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

	public void AddDomainEvent(BaseEvent domainEvent)
	{
		_domainEvents.Add(domainEvent);
	}

	public void RemoveDomainEvent(BaseEvent domainEvent)
	{
		_domainEvents.Remove(domainEvent);
	}

	public void ClearDomainEvents()
	{
		_domainEvents.Clear();
	}
}