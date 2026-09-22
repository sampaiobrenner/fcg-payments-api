using Fcg.Payments.Domain._Shared.Events;

namespace Fcg.Payments.Domain._Shared.Models;

public abstract class DomainModelBase<TPersistenceModel>
    where TPersistenceModel : PersistenceModelBase
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected DomainModelBase(TPersistenceModel persistenceModel)
    {
        ArgumentNullException.ThrowIfNull(persistenceModel);
        PersistenceModel = persistenceModel;
    }

    public TPersistenceModel PersistenceModel { get; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public Guid Id() => PersistenceModel.Id;

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
