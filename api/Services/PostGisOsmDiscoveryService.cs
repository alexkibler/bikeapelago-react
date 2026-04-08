using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Bikeapelago.Api.Services;

public class PostGisOsmDiscoveryService : IOsmDiscoveryService
{
    private readonly ILogger<PostGisOsmDiscoveryService> _logger;
    private readonly string _connectionString;
    private readonly HttpClient _httpClient;

    public PostGisOsmDiscoveryService(ILogger<PostGisOsmDiscoveryService> logger, IConfiguration config, HttpClient httpClient)
    {
        _logger = logger;
        _connectionString = config.GetConnectionString("OsmDiscovery")
            ?? throw new InvalidOperationException("OsmDiscovery connection string is required.");
        _httpClient = httpClient;
    }

    public async Task<List<DiscoveryPoint>> GetRandomNodesAsync(double lat, double lon, double radiusMeters, int count)
    {
        _logger.LogInformation("Fetching random nodes from PostGIS at {Lat},{Lon} radius {Radius}m", lat, lon, radiusMeters);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        // Find nodes that are part of ways within the search radius.
        // Filter ways first (fewer objects) for better spatial index performance.
        // This ensures nodes are on actual roads/paths, not random points in fields.
        cmd.CommandText = """
            SELECT DISTINCT ST_X(n.geom)::float8, ST_Y(n.geom)::float8
            FROM planet_osm_ways w
            INNER JOIN planet_osm_way_nodes wn ON w.id = wn.way_id
            INNER JOIN planet_osm_nodes n ON wn.node_id = n.id
            WHERE ST_DWithin(
                w.geom::geography,
                ST_SetSRID(ST_MakePoint(@lon, @lat), 4326)::geography,
                @radius
            )
            ORDER BY RANDOM()
            LIMIT @count
            """;
        cmd.Parameters.AddWithValue("lon", lon);
        cmd.Parameters.AddWithValue("lat", lat);
        cmd.Parameters.AddWithValue("radius", radiusMeters);
        cmd.Parameters.AddWithValue("count", count);

        var results = new List<DiscoveryPoint>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new DiscoveryPoint(reader.GetDouble(0), reader.GetDouble(1)));
        }
        return results;
    }

    public async Task<List<ValidateResult>> ValidateNodesAsync(ValidateRequest request)
    {
        _logger.LogInformation("Validating {Count} nodes via GraphHopper for profile {Profile}", request.Points.Length, request.Profile);

        var results = new List<ValidateResult>();
        const double maxDistanceMeters = 20;

        foreach (var point in request.Points)
        {
            try
            {
                var vehicle = request.Profile.ToLower() switch
                {
                    "foot" or "walk" => "foot",
                    "car" => "car",
                    _ => "bike"
                };
                
                // We use the proxied GraphHopper URL or the one from config
                // For simplicity, let's assume it's available via localhost/graphhopper if internal
                var url = $"http://graphhopper:8989/nearest?point={point.Lat},{point.Lon}&vehicle={vehicle}&type=json";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    var coords = root.GetProperty("coordinates");
                    var snappedLon = coords[0].GetDouble();
                    var snappedLat = coords[1].GetDouble();
                    var distanceFromGraphHopper = root.GetProperty("distance").GetDouble();

                    results.Add(new ValidateResult(
                        Original: point,
                        Snapped: new DiscoveryPoint(snappedLon, snappedLat),
                        DistanceMeters: distanceFromGraphHopper,
                        IsValid: distanceFromGraphHopper <= maxDistanceMeters,
                        RoadName: $"Valid {request.Profile} route"
                    ));
                }
                else
                {
                    results.Add(new ValidateResult(Original: point, IsValid: false, Error: "GraphHopper lookup failed"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate point {Lat},{Lon}", point.Lat, point.Lon);
                results.Add(new ValidateResult(Original: point, IsValid: false, Error: ex.Message));
            }
        }

        return results;
    }
}
