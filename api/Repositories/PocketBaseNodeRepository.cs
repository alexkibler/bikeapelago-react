using System.Collections.Generic;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Services;

namespace Bikeapelago.Api.Repositories;

public class PocketBaseNodeRepository(PocketBaseService pb) : IMapNodeRepository
{
    private readonly PocketBaseService _pb = pb;

    public async Task<IEnumerable<MapNode>> GetBySessionIdAsync(string sessionId)
    {
        var response = await _pb.GetAsync<PocketBaseListResponse<MapNode>>(
            "collections/map_nodes/records",
            filter: $"session='{sessionId}'",
            perPage: 500
        );
        return response?.Items ?? [];
    }

    public async Task<MapNode> CreateAsync(MapNode node)
    {
        var data = new
        {
            session = node.SessionId,
            name = node.Name,
            ap_location_id = node.ApLocationId,
            osm_node_id = node.OsmNodeId,
            lat = node.Lat,
            lon = node.Lon,
            state = node.State
        };
        return await _pb.PostAsync<MapNode>("collections/map_nodes/records", data)
               ?? throw new Exception("Failed to create map node");
    }

    public async Task<MapNode> UpdateAsync(MapNode node)
    {
        var data = new { state = node.State, name = node.Name };
        return await _pb.PatchAsync<MapNode>("collections/map_nodes/records", node.Id, data)
               ?? throw new Exception("Failed to update map node");
    }

    public async Task DeleteBySessionIdAsync(string sessionId)
    {
        var nodes = await GetBySessionIdAsync(sessionId);
        foreach (var node in nodes)
            await _pb.DeleteAsync("collections/map_nodes/records", node.Id);
    }
}
