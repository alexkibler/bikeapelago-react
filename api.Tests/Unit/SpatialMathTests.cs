using System;
using System.Collections.Generic;
using System.Linq;
using Bikeapelago.Api.Services;

namespace Bikeapelago.Api.Tests.Unit;

public class SpatialMathTests
{
    private const double CenterLat = 40.4406;
    private const double CenterLon = -79.9959;

    // ── Radius containment ────────────────────────────────────────────────

    [Theory]
    [InlineData(5_000)]
    [InlineData(50_000)]
    [InlineData(500_000)]
    public void AllPointsFallWithinRadius(double radiusMeters)
    {
        var points = PostGisOsmDiscoveryService.GenerateRandomPointsInCircle(
            CenterLat, CenterLon, radiusMeters, count: 1000);

        foreach (var p in points)
        {
            double distMeters = HaversineMeters(CenterLat, CenterLon, p.Lat, p.Lon);
            // Flat-earth approximation introduces up to ~1% error vs Haversine at 500km.
            Assert.True(distMeters <= radiusMeters * 1.02,
                $"Point ({p.Lat:F5},{p.Lon:F5}) is {distMeters:F0}m from center, radius is {radiusMeters}m");
        }
    }

    [Fact]
    public void RequestedCountIsReturned()
    {
        foreach (var count in new[] { 1, 10, 63, 500, 1250 })
        {
            var points = PostGisOsmDiscoveryService.GenerateRandomPointsInCircle(
                CenterLat, CenterLon, 50_000, count);
            Assert.Equal(count, points.Count);
        }
    }

    // ── Longitude correction ──────────────────────────────────────────────
    // At Pittsburgh (~40°N), 1° lon ≈ 85km, 1° lat ≈ 111km.
    // Without cos(lat) correction, the lon spread would be ~30% wider than lat spread.
    // With correction, they should be roughly equal.

    [Fact]
    public void LongitudeSpreadIsCorrectForLatitude()
    {
        var points = PostGisOsmDiscoveryService.GenerateRandomPointsInCircle(
            CenterLat, CenterLon, 50_000, count: 5000);

        double latSpreadKm = (points.Max(p => p.Lat) - points.Min(p => p.Lat)) * 111.0;
        double lonSpreadKm = (points.Max(p => p.Lon) - points.Min(p => p.Lon)) * 85.0;

        // Both spreads should be within 25% of each other (circle, not ellipse)
        double ratio = lonSpreadKm / latSpreadKm;
        Assert.True(ratio is > 0.75 and < 1.25,
            $"Lon/lat spread ratio {ratio:F2} suggests longitude is not corrected for latitude compression");
    }

    // ── Density bias distribution ─────────────────────────────────────────

    [Fact]
    public void DefaultBias_OuterRingsHaveMorePointsThanInner()
    {
        // densityBias=0.5 (default) = uniform area distribution.
        // The outer annulus (50–100% radius) has 3× the area of the inner (0–50%),
        // so it should contain ~3× as many points.
        var points = PostGisOsmDiscoveryService.GenerateRandomPointsInCircle(
            CenterLat, CenterLon, 50_000, count: 5000, densityBias: 0.5);

        var (inner, outer) = SplitByHalfRadius(points, CenterLat, CenterLon, 50_000);

        Assert.True(outer > inner * 2,
            $"Expected outer ring to dominate with bias=0.5, got inner={inner} outer={outer}");
    }

    [Fact]
    public void BiasOne_InnerRingIsNearParityWithOuter()
    {
        // densityBias=1.0 = linear distance, clusters near center.
        // For linear distribution, expected roughly 50/50 split between inner and outer.
        // We ensure it's greater than 45% to account for randomness.
        var points = PostGisOsmDiscoveryService.GenerateRandomPointsInCircle(
            CenterLat, CenterLon, 50_000, count: 5000, densityBias: 1.0);

        var (inner, outer) = SplitByHalfRadius(points, CenterLat, CenterLon, 50_000);

        Assert.True(inner >= outer * 0.9,
            $"Expected inner ring to be near parity with outer with bias=1.0, got inner={inner} outer={outer}");
    }

    [Fact]
    public void BiasOneFive_InnerRingHasMorePointsThanOuter()
    {
        // densityBias=1.5 = heavy clustering near center.
        // P(r <= R/2) = (0.5)^(1/1.5) approx 0.63.
        var points = PostGisOsmDiscoveryService.GenerateRandomPointsInCircle(
            CenterLat, CenterLon, 50_000, count: 5000, densityBias: 1.5);

        var (inner, outer) = SplitByHalfRadius(points, CenterLat, CenterLon, 50_000);

        Assert.True(inner > outer,
            $"Expected inner ring to dominate with bias=1.5, got inner={inner} outer={outer}");
    }

    [Fact]
    public void GoldilocksBias_IsBeweenDefaultAndLinear()
    {
        // densityBias=1.0 should sit between 0.5 (uniform area) and 1.5 (heavy center) in terms of inner/outer ratio.
        var points05 = PostGisOsmDiscoveryService.GenerateRandomPointsInCircle(CenterLat, CenterLon, 50_000, 5000, 0.5);
        var points10 = PostGisOsmDiscoveryService.GenerateRandomPointsInCircle(CenterLat, CenterLon, 50_000, 5000, 1.0);
        var points15 = PostGisOsmDiscoveryService.GenerateRandomPointsInCircle(CenterLat, CenterLon, 50_000, 5000, 1.5);

        double RatioInnerToOuter(List<Bikeapelago.Api.Models.DiscoveryPoint> pts)
        {
            var (inner, outer) = SplitByHalfRadius(pts, CenterLat, CenterLon, 50_000);
            return (double)inner / outer;
        }

        double r05 = RatioInnerToOuter(points05);
        double r10 = RatioInnerToOuter(points10);
        double r15 = RatioInnerToOuter(points15);

        Assert.True(r05 < r10 && r10 < r15,
            $"Linear bias=1.0 ratio {r10:F3} should be between bias=0.5 ({r05:F3}) and bias=1.5 ({r15:F3})");
    }

    [Fact]
    public void GenerateRandomPointsInWedge_AllPointsAreWithinWedge()
    {
        // Test a wedge from 315 to 45 (wraps north)
        double start = 315;
        double end = 45;
        var points = PostGisOsmDiscoveryService.GenerateRandomPointsInWedge(
            CenterLat, CenterLon, 5000, start, end, count: 1000);

        // Access private static methods via reflection for testing if they weren't internal/public
        // But let's just use the same logic here to verify the output distribution
        foreach (var p in points)
        {
            double az = CalculateAzimuth(CenterLat, CenterLon, p.Lat, p.Lon);
            Assert.True(IsInWedge(az, start, end), $"Point at azimuth {az} should be within {start}-{end}");
        }
    }

    [Fact]
    public void GenerateRandomPointsInWedge_EasternWedge()
    {
        double start = 45;
        double end = 135;
        var points = PostGisOsmDiscoveryService.GenerateRandomPointsInWedge(
            CenterLat, CenterLon, 5000, start, end, count: 1000);

        foreach (var p in points)
        {
            double az = CalculateAzimuth(CenterLat, CenterLon, p.Lat, p.Lon);
            Assert.True(IsInWedge(az, start, end), $"Point at azimuth {az} should be within {start}-{end}");
        }
    }

    // ── All four quadrant wedges ───────────────────────────────────────────

    [Theory]
    [InlineData(315, 45,  "North")]
    [InlineData(45,  135, "East")]
    [InlineData(135, 225, "South")]
    [InlineData(225, 315, "West")]
    public void GenerateRandomPointsInWedge_AllQuadrants_AllPointsWithinWedge(double start, double end, string _)
    {
        var points = PostGisOsmDiscoveryService.GenerateRandomPointsInWedge(
            CenterLat, CenterLon, 50_000, start, end, count: 500);

        Assert.Equal(500, points.Count);
        foreach (var p in points)
        {
            double az = CalculateAzimuth(CenterLat, CenterLon, p.Lat, p.Lon);
            Assert.True(IsInWedge(az, start, end),
                $"{_} quadrant: point at azimuth {az:F2} is not within [{start},{end}]");
        }
    }

    // ── minRadiusMeters ───────────────────────────────────────────────────

    [Fact]
    public void GenerateRandomPointsInWedge_WithMinRadius_NoPointWithinMinRadius()
    {
        const double minRadius = 3_000;
        const double maxRadius = 12_000;

        var points = PostGisOsmDiscoveryService.GenerateRandomPointsInWedge(
            CenterLat, CenterLon, maxRadius, 45, 135, count: 1000, densityBias: 0.5, minRadiusMeters: minRadius);

        foreach (var p in points)
        {
            double dist = HaversineMeters(CenterLat, CenterLon, p.Lat, p.Lon);
            Assert.True(dist >= minRadius * 0.99,
                $"Point at distance {dist:F0}m is inside the forbidden hub zone (minRadius={minRadius}m)");
        }
    }

    [Fact]
    public void GenerateRandomPointsInWedge_WithMinRadius_AllPointsWithinMaxRadius()
    {
        const double minRadius = 3_000;
        const double maxRadius = 12_000;

        var points = PostGisOsmDiscoveryService.GenerateRandomPointsInWedge(
            CenterLat, CenterLon, maxRadius, 45, 135, count: 1000, densityBias: 0.5, minRadiusMeters: minRadius);

        foreach (var p in points)
        {
            double dist = HaversineMeters(CenterLat, CenterLon, p.Lat, p.Lon);
            Assert.True(dist <= maxRadius * 1.02,
                $"Point at distance {dist:F0}m exceeds maxRadius={maxRadius}m");
        }
    }

    [Fact]
    public void GenerateRandomPointsInWedge_WithMinRadius_PointsAreEastOfCenter()
    {
        // East wedge (45-135°) with minRadius should still produce points with lon > centerLon.
        // This guards against any direction regression in the presence of the new minRadius parameter.
        const double minRadius = 2_000;
        const double maxRadius = 10_000;

        var points = PostGisOsmDiscoveryService.GenerateRandomPointsInWedge(
            CenterLat, CenterLon, maxRadius, 45, 135, count: 500, densityBias: 0.5, minRadiusMeters: minRadius);

        // Every east-wedge point must be strictly east of center (lon > centerLon).
        // East wedge spans 45–135°, so all vectors have a positive east component.
        foreach (var p in points)
        {
            Assert.True(p.Lon > CenterLon,
                $"East-wedge point at lon={p.Lon:F5} is west of center lon={CenterLon}");
        }
    }

    // Duplicate logic for testing purposes
    private double CalculateAzimuth(double lat1, double lon1, double lat2, double lon2)
    {
        double lat1Rad = lat1 * Math.PI / 180.0;
        double lat2Rad = lat2 * Math.PI / 180.0;
        double dLonRad = (lon2 - lon1) * Math.PI / 180.0;
        double y = Math.Sin(dLonRad) * Math.Cos(lat2Rad);
        double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLonRad);
        double brng = Math.Atan2(y, x);
        return (brng * 180.0 / Math.PI + 360.0) % 360.0;
    }

    private bool IsInWedge(double az, double start, double end)
    {
        if (start < end) return az >= start && az <= end;
        return az >= start || az <= end;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static (int inner, int outer) SplitByHalfRadius(
        List<Bikeapelago.Api.Models.DiscoveryPoint> points, double clat, double clon, double radiusMeters)
    {
        int inner = 0, outer = 0;
        foreach (var p in points)
        {
            if (HaversineMeters(clat, clon, p.Lat, p.Lon) <= radiusMeters / 2)
                inner++;
            else
                outer++;
        }
        return (inner, outer);
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
}
