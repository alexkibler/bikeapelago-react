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
    public void BiasOneFive_InnerRingHasMorePointsThanInner()
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
