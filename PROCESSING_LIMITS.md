# Processing Limits Configuration

This document explains the processing limits configuration for cost control during testing.

## Overview

The processing limits feature helps you control costs by limiting the number of addresses processed per Excel file. This is especially useful during testing to avoid excessive API usage charges.

## Configuration Settings

The processing limits are configured in `appsettings.json` under the `Neshan:ProcessingLimits` section:

```json
{
  "Neshan": {
    "ApiKey": "your-api-key",
    "RateLimit": {
      // ... rate limiting settings
    },
    "ProcessingLimits": {
      "MaxAddressesPerFile": 10,
      "EnableProcessingLimits": true,
      "SkipRemainingAddresses": true,
      "ShowLimitWarning": true
    }
  }
}
```

## Configuration Parameters

### `MaxAddressesPerFile` (Default: 10)
- **Description**: Maximum number of addresses to process from each Excel file
- **Type**: Integer
- **Usage**: Limits processing to save API costs during testing
- **Recommendation**: Set to 5-20 for testing, higher for production

### `EnableProcessingLimits` (Default: true)
- **Description**: Whether to enable processing limits
- **Type**: Boolean
- **Usage**: Master switch to enable/disable all processing limits
- **Recommendation**: Keep enabled for cost control

### `SkipRemainingAddresses` (Default: true)
- **Description**: Whether to skip remaining addresses when limit is reached
- **Type**: Boolean
- **Usage**: When true, stops processing after reaching the limit
- **Recommendation**: Keep enabled to save costs

### `ShowLimitWarning` (Default: true)
- **Description**: Whether to show warnings when limits are applied
- **Type**: Boolean
- **Usage**: Controls logging of limit warnings
- **Recommendation**: Keep enabled for transparency

## How It Works

### 1. Excel File Processing
- When reading Excel files, the service checks the processing limit
- Only processes the first N addresses (where N = MaxAddressesPerFile)
- Skips remaining addresses to save API costs
- Logs warning messages when limits are applied

### 2. Address List Processing
- When processing address lists, applies the same limit
- Takes only the first N addresses from the list
- Skips remaining addresses
- Shows skipped count in response

### 3. Response Information
- API responses include processing limit information
- Shows whether limits were applied
- Displays number of skipped addresses
- Provides transparency about cost savings

## Example Responses

### When Limit is Applied:
```json
{
  "message": "Processing completed. 8 successful, 2 failed.",
  "totalProcessed": 10,
  "successCount": 8,
  "failedCount": 2,
  "processingLimits": {
    "enabled": true,
    "maxAddressesPerFile": 10,
    "limitApplied": true,
    "skippedCount": 15
  }
}
```

### When Limit is Not Applied:
```json
{
  "message": "Processing completed. 5 successful, 0 failed.",
  "totalProcessed": 5,
  "successCount": 5,
  "failedCount": 0,
  "processingLimits": {
    "enabled": true,
    "maxAddressesPerFile": 10,
    "limitApplied": false,
    "skippedCount": 0
  }
}
```

## UI Integration

The web interface shows:
- **Cost Control Notice**: Blue info box explaining the limit
- **Processing Summary**: Shows how many addresses were processed vs. total
- **Limit Warnings**: When limits are applied, shows skipped count
- **Transparency**: Clear indication of cost-saving measures

## Recommended Configurations

### For Testing/Development
```json
{
  "ProcessingLimits": {
    "MaxAddressesPerFile": 5,
    "EnableProcessingLimits": true,
    "SkipRemainingAddresses": true,
    "ShowLimitWarning": true
  }
}
```

### For Production (Cost-Conscious)
```json
{
  "ProcessingLimits": {
    "MaxAddressesPerFile": 20,
    "EnableProcessingLimits": true,
    "SkipRemainingAddresses": true,
    "ShowLimitWarning": true
  }
}
```

### For Production (Unlimited)
```json
{
  "ProcessingLimits": {
    "MaxAddressesPerFile": 1000,
    "EnableProcessingLimits": false,
    "SkipRemainingAddresses": false,
    "ShowLimitWarning": false
  }
}
```

## Cost Savings Example

### Scenario:
- Excel file with 100 addresses
- API cost: $0.01 per request
- Processing limit: 10 addresses

### Without Limits:
- **Cost**: 100 × $0.01 = $1.00
- **Processing**: All 100 addresses

### With Limits:
- **Cost**: 10 × $0.01 = $0.10
- **Processing**: First 10 addresses only
- **Savings**: $0.90 (90% cost reduction)

## Logging

The service provides detailed logging for processing limits:

```
[Information] Excel processing completed: 10/100 addresses processed (limit: 10). Skipped 90 addresses.
[Warning] Processing limit reached. Processed 10/10 addresses. Skipped 90 remaining addresses.
[Information] Processing limit applied. Processing 10/25 addresses. Skipped 15 addresses.
```

## Troubleshooting

### Too Many Addresses Processed
1. **Reduce `MaxAddressesPerFile`** - Lower the limit
2. **Check `EnableProcessingLimits`** - Ensure it's set to `true`
3. **Verify `SkipRemainingAddresses`** - Should be `true`

### No Addresses Processed
1. **Check Excel file format** - Ensure proper headers
2. **Verify address column** - Should contain valid addresses
3. **Check limit settings** - Ensure limits aren't too restrictive

### Performance Issues
1. **Increase `MaxAddressesPerFile`** - Allow more processing
2. **Disable limits temporarily** - Set `EnableProcessingLimits` to `false`
3. **Check rate limiting** - May be affecting performance

## Best Practices

### For Testing
- Use small limits (5-10 addresses)
- Enable all warnings
- Monitor costs closely
- Test with sample data first

### For Production
- Set appropriate limits based on budget
- Monitor usage patterns
- Adjust limits based on actual costs
- Consider batch processing for large files

### For Development
- Use very small limits (2-5 addresses)
- Enable detailed logging
- Test with various file formats
- Monitor API response times

## Monitoring

You can monitor processing limits by checking:
- Application logs for limit warnings
- API response processing limit information
- UI display of limit information
- Cost tracking through API usage

The processing limits provide a cost-effective way to test and use the geocoding service while maintaining control over API usage and costs. 