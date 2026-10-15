using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NeshanGeocodingApi.Data;
using NeshanGeocodingApi.Models;
using NeshanGeocodingApi.Services;

namespace NeshanGeocodingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeocodingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ExcelService _excelService;
        private readonly NeshanGeocodingService _geocodingService;
        private readonly ILogger<GeocodingController> _logger;

        public GeocodingController(
            ApplicationDbContext context,
            ExcelService excelService,
            NeshanGeocodingService geocodingService,
            ILogger<GeocodingController> logger)
        {
            _context = context;
            _excelService = excelService;
            _geocodingService = geocodingService;
            _logger = logger;
        }

        [HttpPost("upload-excel")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
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
                using var stream = file.OpenReadStream();
                var (addresses, totalRows, processedRows) = await _excelService.ReadAddressesFromExcelAsync(stream);

                if (!addresses.Any())
                {
                    return BadRequest("No valid addresses found in the Excel file.");
                }

                // Save addresses to database
                _context.Addresses.AddRange(addresses);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Uploaded {Count} addresses from Excel file (Total rows: {TotalRows}, Processed: {ProcessedRows})", 
                    addresses.Count, totalRows, processedRows);

                return Ok(new
                {
                    message = $"Successfully uploaded {addresses.Count} addresses.",
                    addressesCount = addresses.Count,
                    totalRows = totalRows,
                    processedRows = processedRows,
                    skippedRows = totalRows - processedRows,
                    addresses = addresses.Select(a => new
                    {
                        a.Id,
                        a.FullAddress,
                        a.Status
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Excel file");
                return StatusCode(500, $"Error processing Excel file: {ex.Message}");
            }
        }

        [HttpPost("geocode-pending")]
        public async Task<IActionResult> GeocodePendingAddresses()
        {
            try
            {
                var pendingAddresses = await _context.Addresses
                    .Where(a => a.Status == "Pending")
                    .ToListAsync();

                if (!pendingAddresses.Any())
                {
                    return Ok(new { message = "No pending addresses to geocode." });
                }

                _logger.LogInformation("Starting geocoding for {Count} pending addresses", pendingAddresses.Count);

                var geocodedAddresses = await _geocodingService.GeocodeAddressesAsync(pendingAddresses);

                // Update database with geocoding results
                foreach (var address in geocodedAddresses)
                {
                    var existingAddress = await _context.Addresses.FindAsync(address.Id);
                    if (existingAddress != null)
                    {
                        existingAddress.Latitude = address.Latitude;
                        existingAddress.Longitude = address.Longitude;
                        existingAddress.GeocodedAddress = address.GeocodedAddress;
                        existingAddress.Status = address.Status;
                        existingAddress.ErrorMessage = address.ErrorMessage;
                        existingAddress.UpdatedAt = address.UpdatedAt;
                    }
                }

                await _context.SaveChangesAsync();

                var successCount = geocodedAddresses.Count(a => a.Status == "Success");
                var failedCount = geocodedAddresses.Count(a => a.Status == "Failed");

                _logger.LogInformation("Geocoding completed: {SuccessCount} success, {FailedCount} failed", 
                    successCount, failedCount);

                return Ok(new
                {
                    message = $"Geocoding completed. {successCount} successful, {failedCount} failed.",
                    totalProcessed = geocodedAddresses.Count,
                    successCount,
                    failedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during geocoding process");
                return StatusCode(500, $"Error during geocoding: {ex.Message}");
            }
        }

        [HttpGet("addresses")]
        public async Task<IActionResult> GetAddresses([FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.Addresses.AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(a => a.Status == status);
                }

                var totalCount = await query.CountAsync();
                var addresses = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    addresses,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving addresses");
                return StatusCode(500, $"Error retrieving addresses: {ex.Message}");
            }
        }

        [HttpGet("addresses/{id}")]
        public async Task<IActionResult> GetAddress(int id)
        {
            try
            {
                var address = await _context.Addresses.FindAsync(id);
                if (address == null)
                {
                    return NotFound($"Address with ID {id} not found.");
                }

                return Ok(address);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving address {Id}", id);
                return StatusCode(500, $"Error retrieving address: {ex.Message}");
            }
        }

        [HttpDelete("addresses/{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            try
            {
                var address = await _context.Addresses.FindAsync(id);
                if (address == null)
                {
                    return NotFound($"Address with ID {id} not found.");
                }

                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Address with ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting address {Id}", id);
                return StatusCode(500, $"Error deleting address: {ex.Message}");
            }
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var totalAddresses = await _context.Addresses.CountAsync();
                var pendingAddresses = await _context.Addresses.CountAsync(a => a.Status == "Pending");
                var successfulAddresses = await _context.Addresses.CountAsync(a => a.Status == "Success");
                var failedAddresses = await _context.Addresses.CountAsync(a => a.Status == "Failed");

                return Ok(new
                {
                    totalAddresses,
                    pendingAddresses,
                    successfulAddresses,
                    failedAddresses,
                    successRate = totalAddresses > 0 ? (double)successfulAddresses / totalAddresses * 100 : 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving statistics");
                return StatusCode(500, $"Error retrieving statistics: {ex.Message}");
            }
        }
    }
} 