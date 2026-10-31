using MassTransit.Testing;

namespace Fcg.Payments.IntegrationTests._Fixtures;

public static class HarnessExtensions
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Aguarda ate que <paramref name="expectedCount"/> mensagens consumidas satisfacam o filtro. Faz polling porque
    /// a entrega pelo outbox acontece depois do commit e pode passar da janela de inatividade do harness.
    /// </summary>
    public static async Task<IReadOnlyList<IReceivedMessage<T>>> WaitForConsumedAsync<T>(this ITestHarness harness, Func<T, bool> filter, int expectedCount = 1)
        where T : class
    {
        var deadline = DateTime.UtcNow + DefaultTimeout;

        while (true)
        {
            var messages = harness.Consumed.Select<T>()
                .Where(received => filter(received.Context.Message))
                .ToList();

            if (messages.Count >= expectedCount || DateTime.UtcNow > deadline)
                return messages;

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }
    }

    public static async Task<IReadOnlyList<T>> WaitForPublishedEventsAsync<T>(this ITestHarness harness, Func<T, bool> filter, int expectedCount = 1)
        where T : class
        => (await harness.WaitForConsumedAsync(filter, expectedCount)).Select(received => received.Context.Message).ToList();
}
