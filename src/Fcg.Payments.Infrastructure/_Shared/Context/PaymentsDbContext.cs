using Fcg.Payments.Application._Shared.Contexts;
using Fcg.Payments.Domain._Shared.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Payments.Infrastructure._Shared.Context;

public sealed class PaymentsDbContext : DbContext, IPaymentsDbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options)
    {
    }

    public IQueryable<T> DataSet<T>() where T : PersistenceModelBase => Set<T>().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
