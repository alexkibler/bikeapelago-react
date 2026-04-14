using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bikeapelago.Api.Models;

/// <summary>
/// Represents a geographic coordinate (lon, lat) for routing with Mapbox APIs.
/// </summary>
public class MapboxCoordinate
{
    [JsonPropertyName("lon")]
    public double Longitude { get; set; }

    [JsonPropertyName("lat")]
    public double Latitude { get; set; }

    public MapboxCoordinate() { }

    public MapboxCoordinate(double longitude, double latitude)
    {
        Longitude = longitude;
        Latitude = latitude;
    }

    public override string ToString() => $"{Longitude},{Latitude}";
}

/// <summary>
/// Response from Mapbox Optimization API.
/// </summary>
public class MapboxOptimizationResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("trips")]
    public List<MapboxTrip> Trips { get; set; } = new();

    [JsonPropertyName("waypoints")]
    public List<MapboxWaypoint> Waypoints { get; set; } = new();
}

/// <summary>
/// A single optimized trip/route from the Mapbox Optimization API.
/// </summary>
public class MapboxTrip
{
    [JsonPropertyName("geometry")]
    public JsonElement? GeometryRaw { get; set; }

    [JsonPropertyName("legs")]
    public List<MapboxLeg> Legs { get; set; } = new();

    [JsonPropertyName("distance")]
    public double Distance { get; set; }

    [JsonPropertyName("duration")]
    public double Duration { get; set; }
    public List<List<double>> GetCoordinates()
    {
        var list = new List<List<double>>();
        if (GeometryRaw.HasValue && GeometryRaw.Value.TryGetProperty("coordinates", out var coordsElement))
        {
            if (coordsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var pointElement in coordsElement.EnumerateArray())
                {
                    if (pointElement.ValueKind == JsonValueKind.Array && pointElement.GetArrayLength() >= 2)
                    {
                        var lon = pointElement[0].GetDouble();
                        var lat = pointElement[1].GetDouble();
                        list.Add(new List<double> { lon, lat });
                    }
                }
            }
        }
        return list;
    }
}

/// <summary>
/// A leg of the trip (segment between two waypoints).
/// </summary>
public class MapboxLeg
{
    [JsonPropertyName("distance")]
    public double Distance { get; set; }

    [JsonPropertyName("duration")]
    public double Duration { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// A waypoint in the optimization response, showing the snapped/matched location.
/// </summary>
public class MapboxWaypoint
{
    [JsonPropertyName("hint")]
    public string? Hint { get; set; }

    [JsonPropertyName("distance")]
    public double Distance { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public List<double> Location { get; set; } = new();

    /// <summary>
    /// Gets the longitude from the location array [lon, lat].
    /// </summary>
    [JsonIgnore]
    public double Longitude => Location.Count > 0 ? Location[0] : 0;

    /// <summary>
    /// Gets the latitude from the location array [lon, lat].
    /// </summary>
    [JsonIgnore]
    public double Latitude => Location.Count > 1 ? Location[1] : 0;
}

/// <summary>
/// Result of a complete optimized route through multiple nodes/waypoints.
/// </summary>
public class OptimizedRouteResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Concatenated geometry coordinates from all trip segments.
    /// Each entry is [lon, lat].
    /// </summary>
    [JsonPropertyName("geometry")]
    public List<List<double>> Geometry { get; set; } = new();

    /// <summary>
    /// Ordered list of node IDs representing the optimized visit order.
    /// </summary>
    [JsonPropertyName("ordered_node_ids")]
    public List<Guid> OrderedNodeIds { get; set; } = new();

    /// <summary>
    /// Snapped road-network locations for each node, keyed by node ID.
    /// When the routing engine moved a node to the nearest road, the snapped
    /// [lon, lat] is stored here. Nodes that were already on the road will
    /// still appear here with their matched location.
    /// </summary>
    [JsonPropertyName("snapped_locations")]
    public Dictionary<Guid, List<double>> SnappedLocations { get; set; } = new();

    /// <summary>
    /// Total distance in meters for the complete route.
    /// </summary>
    [JsonPropertyName("total_distance_meters")]
    public double TotalDistanceMeters { get; set; }

    /// <summary>
    /// Total duration in seconds for the complete route.
    /// </summary>
    [JsonPropertyName("total_duration_seconds")]
    public double TotalDurationSeconds { get; set; }
}

public class RouteRequest
{
    [JsonPropertyName("waypoints")]
    public List<MapboxCoordinate> Waypoints { get; set; } = new();

    [JsonPropertyName("profile")]
    public string Profile { get; set; } = "cycling";
}
