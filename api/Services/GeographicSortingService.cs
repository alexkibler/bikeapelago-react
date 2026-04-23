using System;
using System.Collections.Generic;
using System.Linq;
using Bikeapelago.Api.Models;
using NetTopologySuite.Geometries;

namespace Bikeapelago.Api.Services;

/// <summary>
/// Service for sorting geographic points using geographic proximity algorithms.
/// </summary>
public class GeographicSortingService : IGeographicSortingService
{
    public List<MapNode> SortByNearestNeighbor(Point startingLocation, List<MapNode> nodes)
    {
        if (nodes == null || nodes.Count == 0)
            return new List<MapNode>();

        if (startingLocation == null)
            throw new ArgumentException("Starting location must have valid coordinates");

        var sorted = new List<MapNode>();
        var remaining = new List<MapNode>(nodes);

        // Current position (start at user location)
        var currentLat = startingLocation.Y;
        var currentLon = startingLocation.X;

        while (remaining.Count > 0)
        {
            // Find nearest unvisited node
            var nearestNode = remaining
                .OrderBy(n => HaversineDistance(currentLat, currentLon, n.Location!.Y, n.Location.X))
                .First();

            sorted.Add(nearestNode);
            remaining.Remove(nearestNode);

            // Move to the newly visited node
            currentLat = nearestNode.Location!.Y;
            currentLon = nearestNode.Location.X;
        }

        return sorted;
    }

    private double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusMeters = 6371000;

        var lat1Rad = lat1 * Math.PI / 180;
        var lat2Rad = lat2 * Math.PI / 180;
        var deltaLat = (lat2 - lat1) * Math.PI / 180;
        var deltaLon = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }
}
