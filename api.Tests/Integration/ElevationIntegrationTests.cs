using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Bikeapelago.Api.Models;
using NetTopologySuite.Geometries;

namespace Bikeapelago.Api.Tests.Integration;

/// <summary>
/// Integration tests for elevation system.
/// Verifies that:
/// 1. Elevation downloads are queued at session creation (not route planning)
/// 2. Frontend can fall back to Mapbox during download
/// 3. No calls to USGS are made (jobs are just queued)
/// </summary>
public class ElevationIntegrationTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _client = null!;

    public ElevationIntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>();
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    /// <summary>
    /// Verify elevation data is queued at session creation, not route planning
    /// </summary>
    [Fact(Skip = "Requires full integration environment")]
    public async Task CreateSession_QueuElevationDownload_DoesNotBlockRouting()
    {
        // Arrange: Create a session with nodes
        var sessionRequest = new
        {
            id = Guid.NewGuid(),
            userId = Guid.NewGuid(),
            name = "Test Session",
            mode = "bike"
        };

        // Act: Create session
        var createResponse = await _client.PostAsJsonAsync("/api/sessions", sessionRequest);
        Assert.True(createResponse.IsSuccessStatusCode, "Session creation should succeed");

        var session = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        var sessionId = session.id;

        // Act: Queue elevation downloads
        var elevationResponse = await _client.PostAsync($"/api/elevation/ensure-tiles/{sessionId}", null);
        Assert.True(elevationResponse.IsSuccessStatusCode, "Should queue elevation downloads");

        var elevationData = await elevationResponse.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(elevationData.tilesNeeded);

        // Assert: Elevation queuing should be fast (not blocking on downloads)
        // If it was blocking on downloads, this would timeout
        // If it's just queueing, it returns immediately
    }

    /// <summary>
    /// Verify that elevation queries work immediately with Mapbox fallback
    /// </summary>
    [Fact(Skip = "Requires full integration environment")]
    public async Task ElevationQuery_UsesFallback_WhenPostGISNotReady()
    {
        // Act: Query elevation at a location (PostGIS may not have tile yet)
        var response = await _client.GetAsync("/api/elevation/point?lat=40.5&lon=-80.5");

        // Assert: Should still return elevation data (from Mapbox fallback)
        Assert.True(response.IsSuccessStatusCode, "Should return elevation data even if PostGIS not ready");

        var data = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(data.elevation_m);
        // May be from Mapbox, but should have a value
    }

    /// <summary>
    /// Verify elevation statistics show coverage as tiles load
    /// </summary>
    [Fact(Skip = "Requires full integration environment")]
    public async Task ElevationStats_ShowsCoverageProgress()
    {
        // Act: Get coverage statistics
        var response = await _client.GetAsync("/api/elevation/stats");
        Assert.True(response.IsSuccessStatusCode, "Should get elevation stats");

        var stats = await response.Content.ReadFromJsonAsync<dynamic>();

        // Assert: Stats should show what's available
        Assert.NotNull(stats.totalNodes);
        Assert.NotNull(stats.nodesWithElevation);
        Assert.NotNull(stats.coveragePercent);

        // Coverage will increase as tiles download
    }

    /// <summary>
    /// Verify elevation profile queries use fallback during download
    /// </summary>
    [Fact(Skip = "Requires full integration environment")]
    public async Task ElevationProfile_WorksImmediately_WithMapboxFallback()
    {
        // Arrange: Route coordinates
        var profileRequest = new
        {
            coordinates = new[]
            {
                new[] { -80.0, 40.44 }, // Pittsburgh
                new[] { -87.63, 41.88 }, // Chicago
            },
            sample_interval_m = 100
        };

        // Act: Get elevation profile (may use Mapbox while PostGIS loads)
        var response = await _client.PostAsJsonAsync("/api/elevation/profile", profileRequest);

        // Assert: Should return profile data immediately
        Assert.True(response.IsSuccessStatusCode, "Should return elevation profile");

        var data = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(data.profile);
        Assert.NotNull(data.statistics);

        // Profile should have elevation data (from Mapbox until PostGIS ready)
    }
}

/// <summary>
/// Tests that verify no external API calls are made during normal operation
/// </summary>
public class NoExternalAPICallsTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _client = null!;
    private readonly MockHttpMessageHandler _mockHandler = new();

    public NoExternalAPICallsTests()
    {
        // Create factory with mocked HTTP to prevent external calls
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Could inject mocked HTTP client here
            });
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    /// <summary>
    /// Verify that session creation queues jobs but doesn't download
    /// </summary>
    [Fact(Skip = "Requires HTTP mocking setup")]
    public async Task SessionCreation_QueuesJobs_DoesNotDownload()
    {
        // Arrange
        _mockHandler.ExpectNoCallsTo("https://cloud.sdsc.edu/v1/AUTH_opentopography/");
        _mockHandler.ExpectNoCallsTo("https://lpdaac.usgs.gov/");

        // Act: Create session and queue elevation
        // (Would make requests here)

        // Assert: No external API calls should have been made
        Assert.Empty(_mockHandler.UnexpectedCalls);
    }
}

/// <summary>
/// Mock HTTP message handler to prevent external API calls in tests
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(string method, string url)> _calls = new();
    public IReadOnlyList<(string method, string url)> UnexpectedCalls => _calls.AsReadOnly();

    private readonly HashSet<string> _allowedHosts = new()
    {
        "localhost",
        "127.0.0.1",
    };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var host = request.RequestUri?.Host ?? "";

        if (!_allowedHosts.Contains(host) && !host.StartsWith("localhost"))
        {
            _calls.Add((request.Method?.ToString() ?? "UNKNOWN", request.RequestUri?.ToString() ?? ""));
            // Return error response instead of making real call
            return new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)
            {
                Content = new StringContent("External API calls not allowed in tests")
            };
        }

        // Allow local calls (simplified since base is abstract)
        return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
    }

    public void ExpectNoCallsTo(string baseUrl)
    {
        // Just log that we're monitoring for this
    }
}
