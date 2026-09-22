using Fcg.Payments.Infrastructure._Shared.Context;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Payments.WebApi._Shared.Database;

public static class MigrationExtensions
{
    private const string ApplyMigrationsOnStartupKey = "Database:ApplyMigrationsOnStartup";

    public static async Task ApplyMigrationsAsync(this WebApplication app, CancellationToken cancellationToken)
    {
        if (!app.Configuration.GetValue<bool>(ApplyMigrationsOnStartupKey))
            return;

        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
