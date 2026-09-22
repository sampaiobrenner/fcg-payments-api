using Fcg.Payments.Domain._Shared.Models;

namespace Fcg.Payments.Application._Shared.Contexts;

public interface IPaymentsDbContext
{
    IQueryable<T> DataSet<T>() where T : PersistenceModelBase;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
