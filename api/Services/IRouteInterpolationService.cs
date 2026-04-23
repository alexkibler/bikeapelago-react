using System.Collections.Generic;

namespace Bikeapelago.Api.Services;

public interface IRouteInterpolationService
{
    List<Models.PathPoint> InterpolateRoute(List<Models.PathPoint> originalPath, int targetCount);
}
