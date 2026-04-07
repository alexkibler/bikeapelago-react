using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Bikeapelago.Api.Services;

public class OverpassResponse
{
    [JsonPropertyName("elements")]
    public List<OverpassElement> Elements { get; set; } = new();
}

public class OverpassElement
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("lat")]
    public double? Lat { get; set; }

    [JsonPropertyName("lon")]
    public double? Lon { get; set; }

    [JsonPropertyName("nodes")]
    public List<long>? Nodes { get; set; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; set; }
}

public class OsmNode
{
    public long Id { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public Dictionary<string, string>? Tags { get; set; }
}

public class OverpassService(HttpClient httpClient, Microsoft.Extensions.Configuration.IConfiguration configuration)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _overpassUrl = configuration["OverpassUrl"] ?? "https://overpass-api.de/api/interpreter";

    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public async Task<List<OsmNode>> FetchCyclingIntersectionsAsync(double lat, double lon, double radius)
    {
        var query = $$"""
            [out:json][timeout:25];
            way["highway"~"^(residential|tertiary|unclassified|living_street|cycleway|track)$"]["bicycle"!="no"](around:{{radius}},{{lat}},{{lon}});
            (._;>;);
            out;
        """;

        var request = new HttpRequestMessage(HttpMethod.Post, _overpassUrl)
        {
            Content = new StringContent(query, Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<OverpassResponse>(content) ?? new OverpassResponse();

        return ProcessOverpassResponse(data, lat, lon, radius);
    }

    private List<OsmNode> ProcessOverpassResponse(OverpassResponse data, double lat, double lon, double radius)
    {
        var nodes = new Dictionary<long, OsmNode>();
        var ways = new List<OverpassElement>();

        foreach (var el in data.Elements)
        {
            if (el.Type == "node" && el.Lat.HasValue && el.Lon.HasValue)
            {
                nodes[el.Id] = new OsmNode { Id = el.Id, Lat = el.Lat.Value, Lon = el.Lon.Value, Tags = el.Tags };
            }
            else if (el.Type == "way" && el.Nodes != null)
            {
                ways.Add(el);
            }
        }

        var nodeWayNames = new Dictionary<long, HashSet<string>>();

        foreach (var way in ways)
        {
            var wayIdentifier = way.Tags != null && way.Tags.ContainsKey("name") ? way.Tags["name"] : $"way-{way.Id}";

            foreach (var nodeId in way.Nodes!)
            {
                if (!nodeWayNames.ContainsKey(nodeId))
                {
                    nodeWayNames[nodeId] = new HashSet<string>();
                }
                nodeWayNames[nodeId].Add(wayIdentifier);
            }
        }

        var intersections = new List<OsmNode>();

        foreach (var kvp in nodeWayNames)
        {
            var nodeId = kvp.Key;
            var wayNamesSet = kvp.Value;

            if (wayNamesSet.Count >= 2 && nodes.TryGetValue(nodeId, out var node))
            {
                if (HaversineDistance(lat, lon, node.Lat, node.Lon) <= radius)
                {
                    intersections.Add(node);
                }
            }
        }

        return intersections;
    }
}
