using Microsoft.AspNetCore.Mvc;
using NeshanGeocodingApi.Models;
using NeshanGeocodingApi.Services;

namespace NeshanGeocodingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExcelGeocodingController : ControllerBase
    {
        private readonly ExcelService _excelService;
        private readonly NeshanGeocodingService _geocodingService;
        private readonly ExcelExportService _exportService;
        private readonly LiveLogService _liveLogService;
        private readonly ILogger<ExcelGeocodingController> _logger;
        private readonly IConfiguration _configuration;

        public ExcelGeocodingController(
            ExcelService excelService,
            NeshanGeocodingService geocodingService,
            ExcelExportService exportService,
            LiveLogService liveLogService,
            ILogger<ExcelGeocodingController> logger,
            IConfiguration configuration)
        {
            _excelService = excelService;
            _geocodingService = geocodingService;
            _exportService = exportService;
            _liveLogService = liveLogService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("process-and-export")]
        public async Task<IActionResult> ProcessAndExportToExcel(IFormFile file, [FromQuery] bool simpleFormat = false)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
            {
                return BadRequest("Please upload an Excel file (.xlsx or .xls).");
            }

            try
            {
                _liveLogService.AddLog($"📁 Starting Excel file processing: {file.FileName}", "info", new { fileName = file.FileName, fileSize = file.Length });

                // Read addresses from uploaded file
                using var stream = file.OpenReadStream();
                var (addresses, totalRows, processedRows) = await _excelService.ReadAddressesFromExcelAsync(stream);

                if (!addresses.Any())
                {
                    _liveLogService.AddLog("❌ No valid addresses found in Excel file", "error");
                    return BadRequest("No valid addresses found in the Excel file.");
                }

                _liveLogService.AddLog($"📊 Excel file processed: {processedRows}/{totalRows} addresses extracted", "info", new { totalRows, processedRows, skippedRows = totalRows - processedRows });

                _logger.LogInformation("Processing {Count} addresses from Excel file (Total rows: {TotalRows}, Processed: {ProcessedRows})", 
                    addresses.Count, totalRows, processedRows);

                // Geocode all addresses
                _liveLogService.AddLog("🌍 Starting geocoding process...", "info", new { addressCount = addresses.Count });
                
                var geocodedAddresses = await _geocodingService.GeocodeAddressesAsync(addresses);

                var successCount = geocodedAddresses.Count(a => a.Status == "Success");
                var failedCount = geocodedAddresses.Count(a => a.Status == "Failed");

                _liveLogService.AddLog($"✅ Geocoding completed: {successCount} success, {failedCount} failed", "success", new { successCount, failedCount, totalProcessed = geocodedAddresses.Count });

                // Export to Excel
                _liveLogService.AddLog("📄 Starting Excel export...", "info", new { format = simpleFormat ? "Simple" : "Detailed" });
                
                string outputPath;
                if (simpleFormat)
                {
                    outputPath = await _exportService.ExportSimpleResultsToExcelAsync(geocodedAddresses);
                }
                else
                {
                    outputPath = await _exportService.ExportGeocodingResultsToExcelAsync(geocodedAddresses);
                }

                // Get filename for download
                var fileName = Path.GetFileName(outputPath);

                _liveLogService.AddLog($"📁 Excel export completed: {fileName}", "success", new { fileName, filePath = outputPath });

                // Get processing limit info
                var enableLimits = _configuration.GetValue<bool>("Neshan:ProcessingLimits:EnableProcessingLimits", true);
                var maxAddresses = _configuration.GetValue<int>("Neshan:ProcessingLimits:MaxAddressesPerFile", 10);
                var skippedRows = totalRows - processedRows;

                _logger.LogInformation("Export completed: {SuccessCount} success, {FailedCount} failed", 
                    successCount, failedCount);

                var response = new
                {
                    message = $"Processing completed. {successCount} successful, {failedCount} failed.",
                    totalRows = totalRows,
                    processedRows = processedRows,
                    skippedRows = skippedRows,
                    totalProcessed = geocodedAddresses.Count,
                    successCount,
                    failedCount,
                    downloadUrl = $"/exports/{fileName}",
                    fileName = fileName,
                    processingLimits = enableLimits ? new
                    {
                        enabled = true,
                        maxAddressesPerFile = maxAddresses,
                        limitApplied = processedRows < totalRows,
                        skippedRows = skippedRows,
                        apiCallsMade = processedRows,
                        totalRowsInFile = totalRows
                    } : new
                    {
                        enabled = false,
                        maxAddressesPerFile = 0,
                        limitApplied = false,
                        skippedRows = 0,
                        apiCallsMade = processedRows,
                        totalRowsInFile = totalRows
                    }
                };

                _liveLogService.AddLog("🎉 Processing workflow completed successfully!", "success", new { successCount, failedCount, fileName });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _liveLogService.AddLog($"❌ Error processing Excel file: {ex.Message}", "error", new { error = ex.Message, stackTrace = ex.StackTrace });
                _logger.LogError(ex, "Error processing Excel file");
                return StatusCode(500, $"Error processing Excel file: {ex.Message}");
            }
        }

        [HttpPost("geocode-and-export")]
        public async Task<IActionResult> GeocodeAndExportToExcel([FromBody] List<string> addresses, [FromQuery] bool simpleFormat = false)
        {
            if (addresses == null || !addresses.Any())
            {
                return BadRequest("No addresses provided.");
            }

            try
            {
                _liveLogService.AddLog($"📝 Starting address list processing: {addresses.Count} addresses", "info", new { addressCount = addresses.Count });

                // Get processing limits
                var enableLimits = _configuration.GetValue<bool>("Neshan:ProcessingLimits:EnableProcessingLimits", true);
                var maxAddresses = _configuration.GetValue<int>("Neshan:ProcessingLimits:MaxAddressesPerFile", 10);

                // Apply processing limit if enabled
                var addressesToProcess = addresses;
                var limitApplied = false;
                var skippedCount = 0;
                var totalInputCount = addresses.Count;

                if (enableLimits && addresses.Count > maxAddresses)
                {
                    addressesToProcess = addresses.Take(maxAddresses).ToList();
                    skippedCount = addresses.Count - maxAddresses;
                    limitApplied = true;
                    
                    _liveLogService.AddLog($"⚠️ Processing limit applied: {addressesToProcess.Count}/{totalInputCount} addresses (skipped {skippedCount})", "warning", new { processedCount = addressesToProcess.Count, totalInputCount, skippedCount, maxAddresses });
                    
                    _logger.LogWarning("Processing limit applied. Processing {ProcessedCount}/{TotalCount} addresses. Skipped {SkippedCount} addresses.", 
                        addressesToProcess.Count, addresses.Count, skippedCount);
                }

                // Convert string addresses to Address objects
                var addressObjects = addressesToProcess.Select(addr => new Address
                {
                    FullAddress = addr,
                    Status = "Pending"
                }).ToList();

                _liveLogService.AddLog($"🌍 Starting geocoding for {addressObjects.Count} addresses...", "info", new { addressCount = addressObjects.Count });

                _logger.LogInformation("Processing {Count} addresses (Total input: {TotalInput})", addressObjects.Count, totalInputCount);

                // Geocode all addresses
                var geocodedAddresses = await _geocodingService.GeocodeAddressesAsync(addressObjects);

                var successCount = geocodedAddresses.Count(a => a.Status == "Success");
                var failedCount = geocodedAddresses.Count(a => a.Status == "Failed");

                _liveLogService.AddLog($"✅ Geocoding completed: {successCount} success, {failedCount} failed", "success", new { successCount, failedCount, totalProcessed = geocodedAddresses.Count });

                // Export to Excel
                _liveLogService.AddLog("📄 Starting Excel export...", "info", new { format = simpleFormat ? "Simple" : "Detailed" });
                
                string outputPath;
                if (simpleFormat)
                {
                    outputPath = await _exportService.ExportSimpleResultsToExcelAsync(geocodedAddresses);
                }
                else
                {
                    outputPath = await _exportService.ExportGeocodingResultsToExcelAsync(geocodedAddresses);
                }

                // Get filename for download
                var fileName = Path.GetFileName(outputPath);

                _liveLogService.AddLog($"📁 Excel export completed: {fileName}", "success", new { fileName, filePath = outputPath });

                _logger.LogInformation("Export completed: {SuccessCount} success, {FailedCount} failed", 
                    successCount, failedCount);

                var response = new
                {
                    message = $"Processing completed. {successCount} successful, {failedCount} failed.",
                    totalInputCount = totalInputCount,
                    processedCount = geocodedAddresses.Count,
                    skippedCount = skippedCount,
                    successCount,
                    failedCount,
                    downloadUrl = $"/exports/{fileName}",
                    fileName = fileName,
                    processingLimits = enableLimits ? new
                    {
                        enabled = true,
                        maxAddressesPerFile = maxAddresses,
                        limitApplied = limitApplied,
                        skippedCount = skippedCount,
                        apiCallsMade = geocodedAddresses.Count,
                        totalInputCount = totalInputCount
                    } : new
                    {
                        enabled = false,
                        maxAddressesPerFile = 0,
                        limitApplied = false,
                        skippedCount = 0,
                        apiCallsMade = geocodedAddresses.Count,
                        totalInputCount = totalInputCount
                    },
                    geocodedAddresses = geocodedAddresses.Select(a => new
                    {
                        fullAddress = a.FullAddress,
                        latitude = a.Latitude,
                        longitude = a.Longitude,
                        geocodedAddress = a.GeocodedAddress,
                        status = a.Status,
                        errorMessage = a.ErrorMessage
                    }).ToList()
                };

                _liveLogService.AddLog("🎉 Address list processing completed successfully!", "success", new { successCount, failedCount, fileName });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _liveLogService.AddLog($"❌ Error processing addresses: {ex.Message}", "error", new { error = ex.Message, stackTrace = ex.StackTrace });
                _logger.LogError(ex, "Error processing addresses");
                return StatusCode(500, $"Error processing addresses: {ex.Message}");
            }
        }

        [HttpGet("download/{fileName}")]
        public IActionResult DownloadFile(string fileName)
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "exports", fileName);
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("File not found.");
                }

                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                
                return PhysicalFile(filePath, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FileName}", fileName);
                return StatusCode(500, $"Error downloading file: {ex.Message}");
            }
        }

        [HttpGet("list-exports")]
        public IActionResult ListExports()
        {
            try
            {
                var exportsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "exports");
                
                if (!Directory.Exists(exportsDirectory))
                {
                    return Ok(new { files = new string[0] });
                }

                var files = Directory.GetFiles(exportsDirectory, "*.xlsx")
                    .Select(f => new
                    {
                        fileName = Path.GetFileName(f),
                        fileSize = new FileInfo(f).Length,
                        createdDate = System.IO.File.GetCreationTime(f),
                        downloadUrl = $"/api/excelgeocoding/download/{Path.GetFileName(f)}"
                    })
                    .OrderByDescending(f => f.createdDate)
                    .ToList();

                return Ok(new { files });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing exports");
                return StatusCode(500, $"Error listing exports: {ex.Message}");
            }
        }

        [HttpGet("live-logs")]
        public async Task<IActionResult> GetLiveLogs()
        {
            await _liveLogService.StreamLogsAsync(Response, HttpContext.RequestAborted);
            return new EmptyResult();
        }

        [HttpGet("configuration")]
        public IActionResult GetConfiguration()
        {
            try
            {
                var config = new
                {
                    processingLimits = new
                    {
                        maxAddressesPerFile = _configuration.GetValue<int>("Neshan:ProcessingLimits:MaxAddressesPerFile", 10),
                        enableProcessingLimits = _configuration.GetValue<bool>("Neshan:ProcessingLimits:EnableProcessingLimits", true),
                        skipRemainingAddresses = _configuration.GetValue<bool>("Neshan:ProcessingLimits:SkipRemainingAddresses", true),
                        showLimitWarning = _configuration.GetValue<bool>("Neshan:ProcessingLimits:ShowLimitWarning", true)
                    },
                    rateLimit = new
                    {
                        requestsPerMinute = _configuration.GetValue<int>("Neshan:RateLimit:RequestsPerMinute", 60),
                        delayBetweenRequests = _configuration.GetValue<int>("Neshan:RateLimit:DelayBetweenRequests", 1000),
                        maxConcurrentRequests = _configuration.GetValue<int>("Neshan:RateLimit:MaxConcurrentRequests", 5),
                        retryOnRateLimit = _configuration.GetValue<bool>("Neshan:RateLimit:RetryOnRateLimit", true),
                        maxRetries = _configuration.GetValue<int>("Neshan:RateLimit:MaxRetries", 3),
                        retryDelay = _configuration.GetValue<int>("Neshan:RateLimit:RetryDelay", 2000)
                    }
                };

                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving configuration");
                return StatusCode(500, $"Error retrieving configuration: {ex.Message}");
            }
        }
    }
} 