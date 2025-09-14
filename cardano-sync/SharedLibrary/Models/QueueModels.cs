namespace SharedLibrary.Models
{
    public class QueueStats
    {
        public long QueueLength { get; set; }
        public long ProcessingCount { get; set; }
        public long TotalProcessed { get; set; }
        public long TotalCompleted { get; set; }
        public long TotalFailed { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    public class JobStats
    {
        public string JobType { get; set; } = "";
        public long Queued { get; set; }
        public long Processing { get; set; }
        public long Completed { get; set; }
        public long Retried { get; set; }
        public long Failed { get; set; }
    }

    public class JobRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string JobType { get; set; } = "";
        public string Endpoint { get; set; } = "";
        public Dictionary<string, string>? Headers { get; set; }
        public int Priority { get; set; } = 10; // 1 = highest, 10 = lowest
        public int RetryCount { get; set; } = 0;
        public int MaxRetries { get; set; } = 3;
        public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessingStartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public JobRequestStatus Status { get; set; } = JobRequestStatus.Queued;
    }

    public enum JobRequestStatus
    {
        Queued,
        Processing,
        Completed,
        Failed,
        Retrying
    }

    public class EnhancedQueueStats : QueueStats
    {
        public long TotalRetried { get; set; }
        public Dictionary<string, long> QueueLengthByPriority { get; set; } = new();
        public Dictionary<string, JobStats> JobStats { get; set; } = new();
        public long RateLimitedRequests { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
    }

    public class JobExecutionResult
    {
        public bool Success { get; set; }
        public string? Result { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, string>? ResponseHeaders { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public bool ShouldRetry { get; set; } = false;
    }
}