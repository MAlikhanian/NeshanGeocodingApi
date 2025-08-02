using System.ComponentModel.DataAnnotations;

namespace NeshanGeocodingApi.Models
{
    public class Address
    {
        public int Id { get; set; }
        
        [Required]
        public string FullAddress { get; set; } = string.Empty;
        
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Street { get; set; }
        public string? Alley { get; set; }
        public string? Building { get; set; }
        
        // Geocoding results
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? GeocodedAddress { get; set; }
        public string? Status { get; set; } // Success, Failed, Pending
        public string? ErrorMessage { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
} 