using System.Collections.Generic;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Services;

public interface IOsmDiscoveryService
{
    Task<List<DiscoveryPoint>> GetRandomNodesAsync(double lat, double lon, double radiusMeters, int count, string mode = "bike", double densityBias = 0.5);
    Task<List<DiscoveryPoint>> GetRandomNodesInWedgeAsync(double lat, double lon, double radiusMeters, double startDeg, double endDeg, int count, string mode = "bike", double densityBias = 0.5);
    Task<List<ValidateResult>> ValidateNodesAsync(ValidateRequest request);
}
