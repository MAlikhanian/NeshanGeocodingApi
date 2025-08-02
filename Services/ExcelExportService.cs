using OfficeOpenXml;
using NeshanGeocodingApi.Models;

namespace NeshanGeocodingApi.Services
{
    public class ExcelExportService
    {
        public async Task<string> ExportGeocodingResultsToExcelAsync(List<Address> addresses, string? outputPath = null)
        {
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            // Generate output filename if not provided
            if (string.IsNullOrEmpty(outputPath))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                outputPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "exports", $"geocoding_results_{timestamp}.xlsx");
            }
            
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Geocoding Results");
            
            // Add headers - Address, X, Y, Google Maps Link, Status, other details
            worksheet.Cells[1, 1].Value = "Address";
            worksheet.Cells[1, 2].Value = "X (Longitude)";
            worksheet.Cells[1, 3].Value = "Y (Latitude)";
            worksheet.Cells[1, 4].Value = "Google Maps Link";
            worksheet.Cells[1, 5].Value = "Status";
            worksheet.Cells[1, 6].Value = "Geocoded Address";
            worksheet.Cells[1, 7].Value = "Error Message";
            
            // Style the header row
            using (var range = worksheet.Cells[1, 1, 1, 7])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }
            
            // Add data rows
            int row = 2;
            foreach (var address in addresses)
            {
                worksheet.Cells[row, 1].Value = address.FullAddress; // Original address from input
                worksheet.Cells[row, 2].Value = address.Longitude;
                worksheet.Cells[row, 3].Value = address.Latitude;
                
                // Create Google Maps link if coordinates are available
                string googleMapsLink = "";
                if (address.Latitude.HasValue && address.Longitude.HasValue)
                {
                    googleMapsLink = $"https://www.google.com/maps?q={address.Latitude},{address.Longitude}";
                }
                worksheet.Cells[row, 4].Value = googleMapsLink;
                
                worksheet.Cells[row, 5].Value = address.Status;
                worksheet.Cells[row, 6].Value = address.GeocodedAddress;
                worksheet.Cells[row, 7].Value = address.ErrorMessage;
                
                // Style based on status
                var statusCell = worksheet.Cells[row, 5];
                if (address.Status == "Success")
                {
                    statusCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    statusCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                }
                else if (address.Status == "Failed")
                {
                    statusCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    statusCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightCoral);
                }
                
                // Make Google Maps link clickable if coordinates exist
                if (!string.IsNullOrEmpty(googleMapsLink))
                {
                    var linkCell = worksheet.Cells[row, 4];
                    linkCell.Hyperlink = new Uri(googleMapsLink);
                    linkCell.Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                    linkCell.Style.Font.UnderLine = true;
                }
                
                row++;
            }
            
            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            
            // Save the file
            await package.SaveAsAsync(new FileInfo(outputPath));
            
            return outputPath;
        }
        
        public async Task<string> ExportSimpleResultsToExcelAsync(List<Address> addresses, string? outputPath = null)
        {
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            // Generate output filename if not provided
            if (string.IsNullOrEmpty(outputPath))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                outputPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "exports", $"addresses_xy_{timestamp}.xlsx");
            }
            
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Addresses with Coordinates");
            
            // Add headers - Address, X, Y, Google Maps Link, Status
            worksheet.Cells[1, 1].Value = "Address";
            worksheet.Cells[1, 2].Value = "X";
            worksheet.Cells[1, 3].Value = "Y";
            worksheet.Cells[1, 4].Value = "Google Maps Link";
            worksheet.Cells[1, 5].Value = "Status";
            
            // Style the header row
            using (var range = worksheet.Cells[1, 1, 1, 5])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }
            
            // Add data rows (only successful geocoding results)
            int row = 2;
            foreach (var address in addresses.Where(a => a.Status == "Success" && a.Latitude.HasValue && a.Longitude.HasValue))
            {
                worksheet.Cells[row, 1].Value = address.FullAddress; // Original address from input
                worksheet.Cells[row, 2].Value = address.Longitude;
                worksheet.Cells[row, 3].Value = address.Latitude;
                
                // Create Google Maps link
                string googleMapsLink = $"https://www.google.com/maps?q={address.Latitude},{address.Longitude}";
                worksheet.Cells[row, 4].Value = googleMapsLink;
                worksheet.Cells[row, 5].Value = address.Status;
                
                // Make Google Maps link clickable
                var linkCell = worksheet.Cells[row, 4];
                linkCell.Hyperlink = new Uri(googleMapsLink);
                linkCell.Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                linkCell.Style.Font.UnderLine = true;
                
                row++;
            }
            
            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            
            // Save the file
            await package.SaveAsAsync(new FileInfo(outputPath));
            
            return outputPath;
        }
    }
} 