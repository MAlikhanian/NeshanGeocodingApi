using OfficeOpenXml;
using NeshanGeocodingApi.Models;

namespace NeshanGeocodingApi.Services
{
    public class ExcelService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ExcelService> _logger;

        public ExcelService(IConfiguration configuration, ILogger<ExcelService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(List<Address> addresses, int totalRows, int processedRows)> ReadAddressesFromExcelAsync(Stream fileStream)
        {
            var addresses = new List<Address>();
            
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var package = new ExcelPackage(fileStream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            
            if (worksheet == null)
                throw new InvalidOperationException("No worksheet found in the Excel file.");
            
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            if (rowCount < 2) // At least header + 1 data row
                throw new InvalidOperationException("Excel file must contain at least a header row and one data row.");
            
            // Read header row to determine column mapping
            var columnMapping = new Dictionary<string, int>();
            
            for (int col = 1; col <= worksheet.Dimension!.Columns; col++)
            {
                var headerValue = worksheet.Cells[1, col].Value?.ToString()?.Trim().ToLower();
                if (!string.IsNullOrEmpty(headerValue))
                {
                    columnMapping[headerValue] = col;
                }
            }
            
            // Get processing limits
            var enableLimits = _configuration.GetValue<bool>("Neshan:ProcessingLimits:EnableProcessingLimits", true);
            var maxAddresses = _configuration.GetValue<int>("Neshan:ProcessingLimits:MaxAddressesPerFile", 10);
            var showWarning = _configuration.GetValue<bool>("Neshan:ProcessingLimits:ShowLimitWarning", true);
            
            var processedCount = 0;
            var totalRows = rowCount - 1; // Exclude header row
            var skippedCount = 0;
            
            // Read data rows
            for (int row = 2; row <= rowCount; row++)
            {
                // Check processing limit
                if (enableLimits && processedCount >= maxAddresses)
                {
                    skippedCount = totalRows - processedCount;
                    if (showWarning)
                    {
                        _logger.LogWarning("Processing limit reached. Processed {ProcessedCount}/{MaxAddresses} addresses. Skipped {SkippedCount} remaining addresses.", 
                            processedCount, maxAddresses, skippedCount);
                    }
                    break;
                }
                
                var address = new Address();
                
                // Try to map columns based on common header names
                if (columnMapping.ContainsKey("address") || columnMapping.ContainsKey("fulladdress"))
                {
                    var col = columnMapping.ContainsKey("address") ? columnMapping["address"] : columnMapping["fulladdress"];
                    address.FullAddress = worksheet.Cells[row, col].Value?.ToString()?.Trim() ?? "";
                }
                else if (columnMapping.ContainsKey("آدرس") || columnMapping.ContainsKey("ادرس"))
                {
                    var col = columnMapping.ContainsKey("آدرس") ? columnMapping["آدرس"] : columnMapping["ادرس"];
                    address.FullAddress = worksheet.Cells[row, col].Value?.ToString()?.Trim() ?? "";
                }
                else
                {
                    // If no specific address column found, use the first column
                    address.FullAddress = worksheet.Cells[row, 1].Value?.ToString()?.Trim() ?? "";
                }
                
                // Map other fields if available
                if (columnMapping.ContainsKey("province") || columnMapping.ContainsKey("استان"))
                {
                    var col = columnMapping.ContainsKey("province") ? columnMapping["province"] : columnMapping["استان"];
                    address.Province = worksheet.Cells[row, col].Value?.ToString()?.Trim();
                }
                
                if (columnMapping.ContainsKey("city") || columnMapping.ContainsKey("شهر"))
                {
                    var col = columnMapping.ContainsKey("city") ? columnMapping["city"] : columnMapping["شهر"];
                    address.City = worksheet.Cells[row, col].Value?.ToString()?.Trim();
                }
                
                if (columnMapping.ContainsKey("district") || columnMapping.ContainsKey("منطقه"))
                {
                    var col = columnMapping.ContainsKey("district") ? columnMapping["district"] : columnMapping["منطقه"];
                    address.District = worksheet.Cells[row, col].Value?.ToString()?.Trim();
                }
                
                if (columnMapping.ContainsKey("street") || columnMapping.ContainsKey("خیابان"))
                {
                    var col = columnMapping.ContainsKey("street") ? columnMapping["street"] : columnMapping["خیابان"];
                    address.Street = worksheet.Cells[row, col].Value?.ToString()?.Trim();
                }
                
                if (columnMapping.ContainsKey("alley") || columnMapping.ContainsKey("کوچه"))
                {
                    var col = columnMapping.ContainsKey("alley") ? columnMapping["alley"] : columnMapping["کوچه"];
                    address.Alley = worksheet.Cells[row, col].Value?.ToString()?.Trim();
                }
                
                if (columnMapping.ContainsKey("building") || columnMapping.ContainsKey("ساختمان"))
                {
                    var col = columnMapping.ContainsKey("building") ? columnMapping["building"] : columnMapping["ساختمان"];
                    address.Building = worksheet.Cells[row, col].Value?.ToString()?.Trim();
                }
                
                // Only add if we have a full address
                if (!string.IsNullOrWhiteSpace(address.FullAddress))
                {
                    address.Status = "Pending";
                    addresses.Add(address);
                    processedCount++;
                }
            }
            
            // Log processing summary
            if (enableLimits && processedCount < totalRows)
            {
                _logger.LogInformation("Excel processing completed: {ProcessedCount}/{TotalRows} addresses processed (limit: {MaxAddresses}). Skipped {SkippedCount} addresses.", 
                    processedCount, totalRows, maxAddresses, skippedCount);
            }
            else
            {
                _logger.LogInformation("Excel processing completed: {ProcessedCount}/{TotalRows} addresses processed.", 
                    processedCount, totalRows);
            }
            
            // Simulate async operation to avoid warning
            await Task.Delay(1);
            
            return (addresses, totalRows, processedCount);
        }
    }
} 