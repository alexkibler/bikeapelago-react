using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bikeapelago.Api.Services;

public interface IGridCacheService
{
    (long gridX, long gridY) GetGridCoordinates(double lat, double lon);
    List<(long, long)> GetCoveringGridCells(double lat, double lon, double radiusMeters);
    Task<Dictionary<(long, long), bool>> CheckCacheStatusAsync(List<(long, long)> gridCells, string mode);
    Task<List<Models.DiscoveryPoint>> GetCachedNodesAsync(List<(long, long)> gridCells, string mode);
    Task<List<int>> QueueCacheJobsAsync(List<(long, long)> gridCells, string mode);
    Task BuildCacheForCellAsync(long gridX, long gridY, string mode);
}
