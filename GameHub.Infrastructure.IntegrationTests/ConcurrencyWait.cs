namespace GameHub.Infrastructure.IntegrationTests;

internal static class ConcurrencyWait
{
    public static async Task<T> DrainAsync<T>(Task<T> workers, TimeSpan timeout)
    {
        try { return await workers.WaitAsync(timeout); }
        catch (TimeoutException)
        {
            // WaitAsync does not cancel workers. Do not dispose their database underneath them.
            // SQL/connection timeouts bound normal operations; the runner has a final process deadline.
            try { await workers; }
            catch { /* Preserve the original timeout as the test failure, not a duplicate result. */ }
            throw;
        }
    }
}
