using System.Text.Json;
using NeshanGeocodingApi.Models;

namespace NeshanGeocodingApi.Services
{
    public class NeshanGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NeshanGeocodingService> _logger;
        private readonly RateLimitService _rateLimitService;
        private readonly LiveLogService _liveLogService;

        public NeshanGeocodingService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ILogger<NeshanGeocodingService> logger,
            RateLimitService rateLimitService,
            LiveLogService liveLogService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _rateLimitService = rateLimitService;
            _liveLogService = liveLogService;
        }

        public async Task<Coordinate?> GeocodeAddressAsync(string address)
        {
            return await _rateLimitService.ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var apiKey = _configuration["Neshan:ApiKey"];
                    if (string.IsNullOrEmpty(apiKey))
                    {
                        throw new InvalidOperationException("Neshan API key is not configured.");
                    }

                    var baseUrl = "https://api.neshan.org/v6/geocoding";
                    var encodedAddress = Uri.EscapeDataString(address);
                    var url = $"{baseUrl}?address={encodedAddress}";

                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("Api-Key", apiKey);

                    _logger.LogInformation("Geocoding address: {Address}", address);

                    var response = await _httpClient.SendAsync(request);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError("Neshan API error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                        
                        // Check for rate limit errors
                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            throw new HttpRequestException($"Rate limit exceeded: {errorContent}");
                        }
                        
                        // Try to parse error response
                        try
                        {
                            var errorResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(errorContent);
                            if (errorResponse != null && errorResponse.ContainsKey("error"))
                            {
                                throw new Exception($"Neshan API error: {errorResponse["error"]}");
                            }
                        }
                        catch
                        {
                            // If we can't parse the error, just throw with status code
                            throw new Exception($"Neshan API error: {response.StatusCode}");
                        }
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var geocodingResponse = JsonSerializer.Deserialize<NeshanGeocodingResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (geocodingResponse?.Location != null)
                    {
                        var result = geocodingResponse.Location;
                        _logger.LogInformation("Successfully geocoded address: {Address} -> Lat: {Lat}, Lng: {Lng}", 
                            address, result.Latitude, result.Longitude);
                        return result;
                    }

                    _logger.LogWarning("No geocoding results found for address: {Address}", address);
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error geocoding address: {Address}", address);
                    throw;
                }
            }, $"GeocodeAddress_{address}");
        }

        public async Task<List<Address>> GeocodeAddressesAsync(List<Address> addresses)
        {
            var results = new List<Address>();
            var totalAddresses = addresses.Count;
            
            _liveLogService.AddLog($"🚀 Starting geocoding for {totalAddresses} addresses", "info", new { totalAddresses });

            for (int i = 0; i < addresses.Count; i++)
            {
                var address = addresses[i];
                var progress = i + 1;
                
                _liveLogService.AddLog($"🌍 Processing address {progress}/{totalAddresses}: {address.FullAddress}", "info", new { progress, totalAddresses, address = address.FullAddress });

                try
                {
                    var coordinate = await GeocodeAddressAsync(address.FullAddress);
                    
                    if (coordinate != null)
                    {
                        address.Latitude = coordinate.Latitude;
                        address.Longitude = coordinate.Longitude;
                        address.GeocodedAddress = coordinate.GeocodedAddress;
                        address.Status = "Success";
                        address.UpdatedAt = DateTime.UtcNow;
                        
                        _liveLogService.AddLog($"✅ Address {progress}/{totalAddresses} geocoded successfully", "success", new { progress, totalAddresses, latitude = coordinate.Latitude, longitude = coordinate.Longitude });
                    }
                    else
                    {
                        address.Status = "Failed";
                        address.ErrorMessage = "No coordinates returned from API";
                        address.UpdatedAt = DateTime.UtcNow;
                        
                        _liveLogService.AddLog($"❌ Address {progress}/{totalAddresses} failed: No coordinates returned", "error", new { progress, totalAddresses, address = address.FullAddress });
                    }
                }
                catch (Exception ex)
                {
                    address.Status = "Failed";
                    address.ErrorMessage = ex.Message;
                    address.UpdatedAt = DateTime.UtcNow;
                    
                    _liveLogService.AddLog($"❌ Address {progress}/{totalAddresses} failed: {ex.Message}", "error", new { progress, totalAddresses, address = address.FullAddress, error = ex.Message });
                    
                    _logger.LogError(ex, "Error geocoding address: {Address}", address.FullAddress);
                }
                
                results.Add(address);
            }

            var successCount = results.Count(a => a.Status == "Success");
            var failedCount = results.Count(a => a.Status == "Failed");
            
            _liveLogService.AddLog($"🎯 Geocoding completed: {successCount} success, {failedCount} failed", "success", new { successCount, failedCount, totalAddresses });

            return results;
        }
    }
} 