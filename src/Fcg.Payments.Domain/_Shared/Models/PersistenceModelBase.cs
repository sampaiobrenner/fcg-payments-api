namespace Fcg.Payments.Domain._Shared.Models;

public abstract class PersistenceModelBase
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
}
