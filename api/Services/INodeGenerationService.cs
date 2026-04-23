using System.Threading.Tasks;

namespace Bikeapelago.Api.Services;

public interface INodeGenerationService
{
    Task<int> GenerateNodesAsync(NodeGenerationRequest request);
}
