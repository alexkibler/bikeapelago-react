using System.Collections.Generic;
using Bikeapelago.Api.Models;
using NetTopologySuite.Geometries;

namespace Bikeapelago.Api.Services;

public interface IGeographicSortingService
{
    List<MapNode> SortByNearestNeighbor(Point startingLocation, List<MapNode> nodes);
}
