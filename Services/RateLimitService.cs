using System.Collections.Concurrent;

namespace NeshanGeocodingApi.Services
{
    public class RateLimitService
    {
        private readonly ConcurrentQueue<DateTime> _requestTimes = new();
        private readonly SemaphoreSlim _semaphore;
        private readonly ILogger<RateLimitService> _logger;
        private readonly LiveLogService _liveLogService;
        
        private readonly int _requestsPerMinute;
        private readonly int _delayBetweenRequests;
        private readonly int _maxConcurrentRequests;
        private readonly bool _retryOnRateLimit;
        private readonly int _maxRetries;
        private readonly int _retryDelay;

        public RateLimitService(IConfiguration configuration, ILogger<RateLimitService> logger, LiveLogService liveLogService)
        {
            _logger = logger;
            _liveLogService = liveLogService;
            
            _requestsPerMinute = configuration.GetValue<int>("Neshan:RateLimit:RequestsPerMinute", 60);
            _delayBetweenRequests = configuration.GetValue<int>("Neshan:RateLimit:DelayBetweenRequests", 1000);
            _maxConcurrentRequests = configuration.GetValue<int>("Neshan:RateLimit:MaxConcurrentRequests", 5);
            _retryOnRateLimit = configuration.GetValue<bool>("Neshan:RateLimit:RetryOnRateLimit", true);
            _maxRetries = configuration.GetValue<int>("Neshan:RateLimit:MaxRetries", 3);
            _retryDelay = configuration.GetValue<int>("Neshan:RateLimit:RetryDelay", 2000);
            
            _semaphore = new SemaphoreSlim(_maxConcurrentRequests, _maxConcurrentRequests);
        }

        public async Task WaitForRateLimitAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                // Clean up old requests (older than 1 minute)
                var cutoffTime = DateTime.UtcNow.AddMinutes(-1);
                while (_requestTimes.TryPeek(out var oldestRequest) && oldestRequest < cutoffTime)
                {
                    _requestTimes.TryDequeue(out _);
                }

                // Check if we've hit the rate limit
                if (_requestTimes.Count >= _requestsPerMinute)
                {
                    if (_requestTimes.TryPeek(out var oldestRequest))
                    {
                        var waitTime = oldestRequest.AddMinutes(1) - DateTime.UtcNow;
                        if (waitTime > TimeSpan.Zero)
                        {
                            _liveLogService.AddLog($"⏳ Rate limit reached. Waiting {waitTime.TotalMilliseconds:F0}ms before next request", "warning", new { waitTimeMs = waitTime.TotalMilliseconds, requestsInQueue = _requestTimes.Count });
                            _logger.LogWarning("Rate limit reached. Waiting {WaitTime}ms before next request", waitTime.TotalMilliseconds);
                            await Task.Delay(waitTime);
                        }
                    }
                }

                // Add current request to queue
                _requestTimes.Enqueue(DateTime.UtcNow);
                
                _liveLogService.AddLog($"📡 API call queued. Current queue: {_requestTimes.Count}/{_requestsPerMinute}", "info", new { queueSize = _requestTimes.Count, maxRequests = _requestsPerMinute });

                // Apply delay between requests
                if (_delayBetweenRequests > 0)
                {
                    await Task.Delay(_delayBetweenRequests);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
        {
            var retryCount = 0;
            
            while (true)
            {
                try
                {
                    await WaitForRateLimitAsync();
                    
                    _liveLogService.AddLog($"🔄 Executing {operationName} (attempt {retryCount + 1})", "info", new { operation = operationName, attempt = retryCount + 1 });
                    
                    var result = await operation();
                    
                    _liveLogService.AddLog($"✅ {operationName} completed successfully", "success", new { operation = operationName });
                    
                    return result;
                }
                catch (Exception ex) when (IsRateLimitError(ex) && _retryOnRateLimit && retryCount < _maxRetries)
                {
                    retryCount++;
                    var delay = _retryDelay * retryCount; // Exponential backoff
                    
                    _liveLogService.AddLog($"⚠️ Rate limit error for {operationName}. Retrying in {delay}ms (attempt {retryCount}/{_maxRetries})", "warning", new { operation = operationName, attempt = retryCount, maxRetries = _maxRetries, delay });
                    
                    _logger.LogWarning("Rate limit error for {Operation}. Retrying in {Delay}ms (attempt {RetryCount}/{MaxRetries})", 
                        operationName, delay, retryCount, _maxRetries);
                    
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    _liveLogService.AddLog($"❌ {operationName} failed after {retryCount + 1} attempts: {ex.Message}", "error", new { operation = operationName, attempts = retryCount + 1, error = ex.Message });
                    
                    _logger.LogError(ex, "{Operation} failed after {RetryCount} attempts", operationName, retryCount + 1);
                    throw;
                }
            }
        }

        private bool IsRateLimitError(Exception ex)
        {
            var message = ex.Message.ToLower();
            return message.Contains("rate limit") || 
                   message.Contains("429") || 
                   message.Contains("too many requests") ||
                   message.Contains("quota exceeded");
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }
} 