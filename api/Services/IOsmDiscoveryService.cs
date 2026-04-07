using System.Collections.Generic;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Services;

public interface IOsmDiscoveryService
{
    Task<List<DiscoveryPoint>> GetRandomNodesAsync(double lat, double lon, double radiusMeters, int count);
    Task<List<ValidateResult>> ValidateNodesAsync(ValidateRequest request);
}
