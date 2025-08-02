# Rate Limiting Configuration

This document explains the rate limiting configuration for the Neshan Geocoding API service.

## Configuration Settings

The rate limiting is configured in `appsettings.json` under the `Neshan:RateLimit` section:

```json
{
  "Neshan": {
    "ApiKey": "your-api-key",
    "RateLimit": {
      "RequestsPerMinute": 60,
      "DelayBetweenRequests": 1000,
      "MaxConcurrentRequests": 5,
      "RetryOnRateLimit": true,
      "MaxRetries": 3,
      "RetryDelay": 2000
    }
  }
}
```

## Configuration Parameters

### `RequestsPerMinute` (Default: 60)
- **Description**: Maximum number of API requests allowed per minute
- **Type**: Integer
- **Usage**: The service will track request times and ensure we don't exceed this limit
- **Recommendation**: Set this to match your Neshan API plan limits

### `DelayBetweenRequests` (Default: 1000)
- **Description**: Delay in milliseconds between consecutive API requests
- **Type**: Integer (milliseconds)
- **Usage**: Adds a delay after each request to prevent overwhelming the API
- **Recommendation**: 1000ms (1 second) is usually sufficient

### `MaxConcurrentRequests` (Default: 5)
- **Description**: Maximum number of concurrent API requests
- **Type**: Integer
- **Usage**: Uses a semaphore to limit concurrent requests
- **Recommendation**: Keep this low (3-5) to avoid rate limit issues

### `RetryOnRateLimit` (Default: true)
- **Description**: Whether to automatically retry requests when rate limit is hit
- **Type**: Boolean
- **Usage**: When true, the service will wait and retry failed requests
- **Recommendation**: Keep enabled for better reliability

### `MaxRetries` (Default: 3)
- **Description**: Maximum number of retry attempts for rate-limited requests
- **Type**: Integer
- **Usage**: Limits how many times a request will be retried
- **Recommendation**: 3-5 retries is usually sufficient

### `RetryDelay` (Default: 2000)
- **Description**: Delay in milliseconds before retrying a rate-limited request
- **Type**: Integer (milliseconds)
- **Usage**: Wait time before retrying after hitting rate limit
- **Recommendation**: 2000ms (2 seconds) or higher

## How It Works

### 1. Request Tracking
- The service maintains a queue of request timestamps
- Old timestamps (older than 1 minute) are automatically removed
- New requests are added to the queue

### 2. Rate Limit Checking
- Before each API request, the service checks if we're at the rate limit
- If the limit is reached, it waits until the oldest request is more than 1 minute old
- This ensures we never exceed the configured requests per minute

### 3. Concurrent Request Limiting
- Uses a semaphore to limit concurrent requests
- Only allows the configured number of requests to run simultaneously
- Prevents overwhelming the API with too many concurrent requests

### 4. Automatic Retry
- When a rate limit error is detected, the service automatically retries
- Waits for the configured retry delay before attempting again
- Continues until success or max retries is reached

## Error Detection

The service detects rate limit errors by checking for:
- HTTP 429 (Too Many Requests) status codes
- Error messages containing "rate limit"
- Error messages containing "429"
- Error messages containing "too many requests"
- Error messages containing "quota exceeded"

## Logging

The service provides detailed logging for rate limiting:
- Initialization logs with configuration values
- Warning logs when rate limits are reached
- Retry attempt logs with retry counts
- Error logs for failed requests

## Example Logs

```
[Information] Rate limit service initialized: 60 requests/minute, 1000ms delay, 5 concurrent
[Warning] Rate limit reached. Waiting 30000ms before next request
[Warning] Rate limit error for GeocodeAddress_تهران. Retry 1/3 in 2000ms
[Information] Successfully geocoded address: تهران -> Lat: 35.6892, Lng: 51.3890
```

## Recommended Configurations

### For Free/Starter Plans
```json
{
  "RateLimit": {
    "RequestsPerMinute": 30,
    "DelayBetweenRequests": 2000,
    "MaxConcurrentRequests": 3,
    "RetryOnRateLimit": true,
    "MaxRetries": 3,
    "RetryDelay": 5000
  }
}
```

### For Standard Plans
```json
{
  "RateLimit": {
    "RequestsPerMinute": 60,
    "DelayBetweenRequests": 1000,
    "MaxConcurrentRequests": 5,
    "RetryOnRateLimit": true,
    "MaxRetries": 3,
    "RetryDelay": 2000
  }
}
```

### For High-Volume Usage
```json
{
  "RateLimit": {
    "RequestsPerMinute": 120,
    "DelayBetweenRequests": 500,
    "MaxConcurrentRequests": 10,
    "RetryOnRateLimit": true,
    "MaxRetries": 5,
    "RetryDelay": 1000
  }
}
```

## Troubleshooting

### Rate Limit Issues
1. **Reduce `RequestsPerMinute`** - Lower the limit to match your API plan
2. **Increase `DelayBetweenRequests`** - Add more delay between requests
3. **Reduce `MaxConcurrentRequests`** - Limit concurrent requests
4. **Increase `RetryDelay`** - Wait longer before retrying

### Performance Issues
1. **Increase `MaxConcurrentRequests`** - Allow more concurrent requests
2. **Reduce `DelayBetweenRequests`** - Reduce delay between requests
3. **Increase `RequestsPerMinute`** - If your plan allows it

### Memory Issues
- The service only stores request timestamps for the last minute
- Memory usage is minimal and constant
- No persistent storage of request data

## Monitoring

You can monitor rate limiting by checking the application logs for:
- Rate limit warnings
- Retry attempts
- Successful geocoding operations
- Error messages

The service provides detailed logging to help you understand how the rate limiting is working and identify any issues. 