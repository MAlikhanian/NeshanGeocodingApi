using System.Text.Json.Serialization;

namespace NeshanGeocodingApi.Models
{
    public class NeshanGeocodingResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("location")]
        public Coordinate Location { get; set; } = new();
    }

    public class GeocodingResult
    {
        [JsonPropertyName("formatted_address")]
        public string FormattedAddress { get; set; } = string.Empty;
        
        [JsonPropertyName("geometry")]
        public Geometry Geometry { get; set; } = new();
        
        [JsonPropertyName("place_id")]
        public string PlaceId { get; set; } = string.Empty;
        
        [JsonPropertyName("types")]
        public List<string> Types { get; set; } = new();
    }
    
    public class Geometry
    {
        [JsonPropertyName("location")]
        public Coordinate Coordinate { get; set; } = new();
        
        [JsonPropertyName("location_type")]
        public string LocationType { get; set; } = string.Empty;
    }

    public class Coordinate
    {
        [JsonPropertyName("x")]
        public double Longitude { get; set; }

        [JsonPropertyName("y")]
        public double Latitude { get; set; }

        public string? GeocodedAddress { get; set; }
    }
} 