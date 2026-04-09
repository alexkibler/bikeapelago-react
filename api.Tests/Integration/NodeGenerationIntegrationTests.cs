using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Bikeapelago.Api.Tests.Integration;

/// <summary>
/// Integration tests for node generation. Require the full Docker stack to be running.
/// Set BIKEAPELAGO_API_URL (default: http://bikeapelago-api.orb.local) and
/// BIKEAPELAGO_TOKEN to override defaults.
/// Skip with: dotnet test --filter "Category!=Integration"
/// </summary>
[Trait("Category", "Integration")]
public class NodeGenerationIntegrationTests : IAsyncLifetime
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("BIKEAPELAGO_API_URL")
        ?? "http://bikeapelago-api.orb.local";

    private static readonly string Token =
        Environment.GetEnvironmentVariable("BIKEAPELAGO_TOKEN")
        ?? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjAxOWQ2ZGM0LTQ5NjUtNzhmZi05ZjJhLWYyN2IyMWFmZDNlNiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJha2libGVyMTZAZ21haWwuY29tIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvZW1haWxhZGRyZXNzIjoiYWtpYmxlcjE2QGdtYWlsLmNvbSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IkFkbWluIiwiZXhwIjoxNzc2MzQyNjkyLCJpc3MiOiJiaWtlYXBlbGFnby1hcGkiLCJhdWQiOiJiaWtlYXBlbGFnby1mcm9udGVuZCJ9.aq_Hc147_Oy1RzhH2dZ0ZlxPpCVMTxvJgqJgqTYQTx0";

    private static readonly string UserId = "019d6dc4-4965-78ff-9f2a-f27b21afd3e6";

    private readonly HttpClient _client;
    private readonly List<Guid> _createdSessionIds = [];

    public NodeGenerationIntegrationTests()
    {
        _client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);
        _client.Timeout = TimeSpan.FromMinutes(2);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var id in _createdSessionIds)
        {
            try { await _client.DeleteAsync($"/api/sessions/{id}"); }
            catch { /* best-effort cleanup */ }
        }
        _client.Dispose();
    }

    // ── Original 3 scenarios ──────────────────────────────────────────────

    [Fact]
    public async Task ConnectionPoolStress_150Nodes_5km()
    {
        var result = await GenerateAndFetch(
            centerLat: 40.4406, centerLon: -79.9959,
            radiusMeters: 5_000, nodeCount: 150);

        Assert.Equal(150, result.Nodes.Count);
        Assert.All(result.Nodes, n => AssertWithinRadius(n, 40.4406, -79.9959, 5_000));
    }

    [Fact]
    public async Task SpatialScale_25Nodes_50km_DistributedAcrossRadius()
    {
        var result = await GenerateAndFetch(
            centerLat: 40.4406, centerLon: -79.9959,
            radiusMeters: 50_000, nodeCount: 25);

        Assert.Equal(25, result.Nodes.Count);

        double latSpreadKm = (result.Nodes.Max(n => n.Lat) - result.Nodes.Min(n => n.Lat)) * 111;
        double lonSpreadKm = (result.Nodes.Max(n => n.Lon) - result.Nodes.Min(n => n.Lon)) * 85;
        Assert.True(latSpreadKm > 20, $"Lat spread {latSpreadKm:F1}km is too small for 50km radius — nodes are clustering");
        Assert.True(lonSpreadKm > 20, $"Lon spread {lonSpreadKm:F1}km is too small for 50km radius — nodes are clustering");
    }

    [Fact]
    public async Task NodeDesert_RiverEdge_StillReturnsFullCount()
    {
        // Center is on the Ohio River bank — ~half probes land in water
        var result = await GenerateAndFetch(
            centerLat: 40.4434, centerLon: -80.0082,
            radiusMeters: 3_000, nodeCount: 25);

        Assert.Equal(25, result.Nodes.Count);
    }

    // ── Bigger radius / node count tests ─────────────────────────────────

    [Fact]
    public async Task BigRadius_100km_200Nodes()
    {
        var result = await GenerateAndFetch(40.4406, -79.9959, 100_000, 200);

        Assert.Equal(200, result.Nodes.Count);
        AssertSpread(result.Nodes, 40.4406, -79.9959, minSpreadKm: 60);
    }

    [Fact]
    public async Task BigRadius_200km_500Nodes()
    {
        var result = await GenerateAndFetch(40.4406, -79.9959, 200_000, 500);

        Assert.Equal(500, result.Nodes.Count);
        AssertSpread(result.Nodes, 40.4406, -79.9959, minSpreadKm: 120);
    }

    [Fact]
    public async Task DenseNodes_50km_500Nodes()
    {
        var result = await GenerateAndFetch(40.4406, -79.9959, 50_000, 500);

        Assert.Equal(500, result.Nodes.Count);
        AssertSpread(result.Nodes, 40.4406, -79.9959, minSpreadKm: 30);
    }

    [Fact]
    public async Task BigRadius_500km_1000Nodes_WideSpread()
    {
        var result = await GenerateAndFetch(40.4406, -79.9959, 500_000, 1000);

        Assert.Equal(1000, result.Nodes.Count);
        AssertSpread(result.Nodes, 40.4406, -79.9959, minSpreadKm: 300);
    }

    [Fact]
    public async Task BigRadius_1000km_1000Nodes_VeryWideSpread()
    {
        var result = await GenerateAndFetch(40.4406, -79.9959, 1_000_000, 1000);

        Assert.Equal(1000, result.Nodes.Count);
        AssertSpread(result.Nodes, 40.4406, -79.9959, minSpreadKm: 600);
    }

    [Fact]
    public async Task BigRadius_500km_2000Nodes()
    {
        var result = await GenerateAndFetch(40.4406, -79.9959, 500_000, 2000);

        Assert.Equal(2000, result.Nodes.Count);
        AssertSpread(result.Nodes, 40.4406, -79.9959, minSpreadKm: 300);
    }

    // ── DensityBias knob ──────────────────────────────────────────────────

    [Fact]
    public async Task DensityBias_Goldilocks_MoreCentralNodesThanDefault()
    {
        const double clat = 40.4406, clon = -79.9959, r = 50_000;

        var defaultResult = await GenerateAndFetch(clat, clon, r, nodeCount: 100, densityBias: 0.5);
        var goldilocks = await GenerateAndFetch(clat, clon, r, nodeCount: 100, densityBias: 0.75);

        int InnerCount(List<NodeDto> nodes) =>
            nodes.Count(n => HaversineMeters(clat, clon, n.Lat, n.Lon) <= r / 2);

        Assert.True(InnerCount(goldilocks.Nodes) > InnerCount(defaultResult.Nodes),
            "densityBias=0.75 should produce more nodes in the inner half than 0.5");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<GenerateResult> GenerateAndFetch(
        double centerLat, double centerLon, double radiusMeters, int nodeCount, double densityBias = 0.5)
    {
        // 1. Create session
        var createResp = await _client.PostAsJsonAsync("/api/sessions", new
        {
            center_lat = centerLat,
            center_lon = centerLon,
            name = $"test-{nodeCount}nodes-{radiusMeters / 1000:F0}km",
            radius = (int)radiusMeters,
            status = "SetupInProgress",
            user = UserId
        });
        createResp.EnsureSuccessStatusCode();
        var session = await createResp.Content.ReadFromJsonAsync<SessionDto>()
            ?? throw new InvalidOperationException("Session creation returned null");
        _createdSessionIds.Add(session.Id);

        // 2. Generate nodes
        var genResp = await _client.PostAsJsonAsync($"/api/sessions/{session.Id}/generate", new
        {
            centerLat,
            centerLon,
            radius = radiusMeters,
            nodeCount,
            mode = "singleplayer",
            densityBias
        });
        Assert.True(genResp.IsSuccessStatusCode,
            $"Generate returned {(int)genResp.StatusCode}: {await genResp.Content.ReadAsStringAsync()}");

        // 3. Fetch nodes
        var nodesResp = await _client.GetAsync($"/api/sessions/{session.Id}/nodes");
        nodesResp.EnsureSuccessStatusCode();
        var nodes = await nodesResp.Content.ReadFromJsonAsync<List<NodeDto>>()
            ?? throw new InvalidOperationException("Nodes endpoint returned null");

        return new GenerateResult(nodes);
    }

    private static void AssertWithinRadius(NodeDto node, double clat, double clon, double radiusMeters)
    {
        double dist = HaversineMeters(clat, clon, node.Lat, node.Lon);
        Assert.True(dist <= radiusMeters * 1.05,
            $"Node ({node.Lat:F5},{node.Lon:F5}) is {dist:F0}m from center, radius={radiusMeters}m");
    }

    private static void AssertSpread(List<NodeDto> nodes, double clat, double clon, double minSpreadKm)
    {
        double latSpread = (nodes.Max(n => n.Lat) - nodes.Min(n => n.Lat)) * 111;
        double lonSpread = (nodes.Max(n => n.Lon) - nodes.Min(n => n.Lon)) * 85;
        double maxSpread = Math.Max(latSpread, lonSpread);
        Assert.True(maxSpread >= minSpreadKm,
            $"Max spread {maxSpread:F1}km is below expected {minSpreadKm}km — nodes are clustering");
    }

    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6_371_000;
        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                 + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                 * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private record GenerateResult(List<NodeDto> Nodes);

    private record SessionDto([property: JsonPropertyName("id")] Guid Id);

    private record NodeDto(
        [property: JsonPropertyName("lat")] double Lat,
        [property: JsonPropertyName("lon")] double Lon);
}
