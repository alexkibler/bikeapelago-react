using System.Text.Json.Serialization;

namespace Bikeapelago.Api.Models;

public record DiscoveryPoint(
    [property: JsonPropertyName("lon")] double Lon,
    [property: JsonPropertyName("lat")] double Lat
);

public record ValidateRequest(
    [property: JsonPropertyName("points")] DiscoveryPoint[] Points,
    [property: JsonPropertyName("profile")] string Profile = "bike"
);

public record ValidateResult(
    [property: JsonPropertyName("original")] DiscoveryPoint Original,
    [property: JsonPropertyName("snapped")] DiscoveryPoint? Snapped = null,
    [property: JsonPropertyName("distanceMeters")] double DistanceMeters = 0,
    [property: JsonPropertyName("isValid")] bool IsValid = false,
    [property: JsonPropertyName("roadName")] string? RoadName = null,
    [property: JsonPropertyName("error")] string? Error = null
);
