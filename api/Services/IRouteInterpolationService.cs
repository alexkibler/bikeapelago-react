using System.Collections.Generic;
using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Services;

public interface IRouteInterpolationService
{
    List<PathPoint> InterpolateRoute(List<PathPoint> originalPath, int targetCount);
    (double CenterLat, double CenterLon, double MaxRadius) ComputeBoundingMetrics(List<PathPoint> path);
}
