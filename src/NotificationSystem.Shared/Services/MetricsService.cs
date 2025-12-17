namespace NotificationSystem.Shared.Services;

public interface IMetricsService
{
    void IncrementSentCount();
    void IncrementFailedCount();
    long GetSentCount();
    long GetFailedCount();
    string GetPrometheusMetrics(string workerName);
}

public class MetricsService : IMetricsService
{
    private long _sentCount;
    private long _failedCount;

    public void IncrementSentCount()
    {
        Interlocked.Increment(ref _sentCount);
    }

    public void IncrementFailedCount()
    {
        Interlocked.Increment(ref _failedCount);
    }

    public long GetSentCount() => Interlocked.Read(ref _sentCount);

    public long GetFailedCount() => Interlocked.Read(ref _failedCount);

    public string GetPrometheusMetrics(string workerName)
    {
        var sentCount = GetSentCount();
        var failedCount = GetFailedCount();
        
        return $"# HELP notifications_sent_total Total number of notifications sent by this worker\n" +
               $"# TYPE notifications_sent_total counter\n" +
               $"notifications_sent_total{{worker=\"{workerName}\"}} {sentCount}\n\n" +
               $"# HELP notifications_failed_total Total number of notifications that failed after all retries\n" +
               $"# TYPE notifications_failed_total counter\n" +
               $"notifications_failed_total{{worker=\"{workerName}\"}} {failedCount}\n";
    }
}
