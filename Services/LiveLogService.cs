using System.Collections.Concurrent;

namespace NeshanGeocodingApi.Services
{
    public class LiveLogService
    {
        private readonly ConcurrentQueue<LogEntry> _logEntries = new();
        private readonly ConcurrentDictionary<string, Action<LogEntry>> _subscribers = new();
        private readonly ILogger<LiveLogService> _logger;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public LiveLogService(ILogger<LiveLogService> logger)
        {
            _logger = logger;
        }

        public void AddLog(string message, string type = "info", object? details = null)
        {
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Type = type,
                Message = message,
                Details = details
            };

            _logEntries.Enqueue(logEntry);

            // Keep only last 100 entries
            while (_logEntries.Count > 100)
            {
                _logEntries.TryDequeue(out _);
            }

            // Broadcast to all subscribers
            BroadcastLog(logEntry);
        }

        public void Subscribe(string subscriberId, Action<LogEntry> callback)
        {
            _subscribers.TryAdd(subscriberId, callback);
        }

        public void Unsubscribe(string subscriberId)
        {
            _subscribers.TryRemove(subscriberId, out _);
        }

        private void BroadcastLog(LogEntry logEntry)
        {
            foreach (var subscriber in _subscribers.Values)
            {
                try
                {
                    subscriber(logEntry);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error broadcasting log to subscriber");
                }
            }
        }

        public List<LogEntry> GetRecentLogs(int count = 50)
        {
            return _logEntries.TakeLast(count).ToList();
        }

        public async Task StreamLogsAsync(HttpResponse response, CancellationToken cancellationToken)
        {
            response.Headers.Add("Content-Type", "text/event-stream");
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Connection", "keep-alive");
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Headers", "Cache-Control");

            var subscriberId = Guid.NewGuid().ToString();
            var logQueue = new ConcurrentQueue<LogEntry>();

            // Subscribe to new logs
            Subscribe(subscriberId, log => logQueue.Enqueue(log));

            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            try
            {
                // Send initial logs
                var recentLogs = GetRecentLogs(20);
                foreach (var log in recentLogs)
                {
                    var logJson = System.Text.Json.JsonSerializer.Serialize(log, jsonOptions);
                    await response.WriteAsync($"data: {logJson}\n\n");
                    await response.Body.FlushAsync();
                }

                // Stream new logs
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (logQueue.TryDequeue(out var logEntry))
                    {
                        var logJson = System.Text.Json.JsonSerializer.Serialize(logEntry, jsonOptions);
                        await response.WriteAsync($"data: {logJson}\n\n");
                        await response.Body.FlushAsync();
                    }
                    else
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
            }
            finally
            {
                Unsubscribe(subscriberId);
            }
        }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = "info"; // info, success, warning, error
        public string Message { get; set; } = "";
        public object? Details { get; set; }
    }
} 