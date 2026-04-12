using System;
using System.Collections.Generic;
using System.Linq;
using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Services;

public class RouteInterpolationService
{
    private const double EarthRadiusMeters = 6371e3;

    public List<PathPoint> InterpolateRoute(List<PathPoint> originalPath, int targetCount)
    {
        if (originalPath == null || originalPath.Count < 2 || targetCount < 2)
            return originalPath?.Take(targetCount).ToList() ?? new List<PathPoint>();

        var totalDistance = 0.0;
        var distances = new double[originalPath.Count];
        distances[0] = 0.0;

        for (int i = 1; i < originalPath.Count; i++)
        {
            var p1 = originalPath[i - 1];
            var p2 = originalPath[i];
            var dist = HaversineDistance(p1.Lat, p1.Lon, p2.Lat, p2.Lon);
            totalDistance += dist;
            distances[i] = totalDistance;
        }

        var segmentLength = totalDistance / (targetCount - 1);
        if (segmentLength <= 0)
        {
            return Enumerable.Repeat(originalPath.First(), targetCount).ToList();
        }

        var interpolated = new List<PathPoint> { originalPath.First() };
        var currentTargetDistance = segmentLength;
        int originalIndex = 1;

        while (interpolated.Count < targetCount - 1 && originalIndex < originalPath.Count)
        {
            if (distances[originalIndex] >= currentTargetDistance)
            {
                var p1 = originalPath[originalIndex - 1];
                var p2 = originalPath[originalIndex];

                var d1 = distances[originalIndex - 1];
                var d2 = distances[originalIndex];

                var fraction = d2 == d1 ? 1.0 : (currentTargetDistance - d1) / (d2 - d1);

                interpolated.Add(new PathPoint
                {
                    Lat = p1.Lat + (p2.Lat - p1.Lat) * fraction,
                    Lon = p1.Lon + (p2.Lon - p1.Lon) * fraction,
                    Alt = p1.Alt.HasValue && p2.Alt.HasValue
                          ? p1.Alt + (p2.Alt - p1.Alt) * fraction
                          : p1.Alt ?? p2.Alt
                });

                currentTargetDistance += segmentLength;
            }
            else
            {
                originalIndex++;
            }
        }

        // Add the very last point explicitly to ensure exact match
        interpolated.Add(originalPath.Last());

        return interpolated;
    }

    public (double CenterLat, double CenterLon, double MaxRadius) ComputeBoundingMetrics(List<PathPoint> path)
    {
        if (path == null || path.Count == 0)
            return (0, 0, 0);

        var minLat = path.Min(p => p.Lat);
        var maxLat = path.Max(p => p.Lat);
        var minLon = path.Min(p => p.Lon);
        var maxLon = path.Max(p => p.Lon);

        var centerLat = (minLat + maxLat) / 2.0;
        var centerLon = (minLon + maxLon) / 2.0;

        var maxRadius = path.Max(p => HaversineDistance(centerLat, centerLon, p.Lat, p.Lon));

        return (centerLat, centerLon, maxRadius);
    }

    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var p1 = lat1 * Math.PI / 180;
        var p2 = lat2 * Math.PI / 180;
        var dp = (lat2 - lat1) * Math.PI / 180;
        var dl = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(dp / 2) * Math.Sin(dp / 2) +
                Math.Cos(p1) * Math.Cos(p2) * Math.Sin(dl / 2) * Math.Sin(dl / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusMeters * c;
    }
}
