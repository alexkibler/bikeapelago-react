using System.Collections.Generic;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Repositories;

public class MockNodeRepository : IMapNodeRepository
{
    private static readonly List<MapNode> _nodes = [];

    public Task<IEnumerable<MapNode>> GetBySessionIdAsync(string sessionId)
    {
        return Task.FromResult<IEnumerable<MapNode>>(_nodes.Where(n => n.SessionId == sessionId));
    }

    public Task<MapNode> CreateAsync(MapNode node)
    {
        if (string.IsNullOrEmpty(node.Id))
        {
            node.Id = Guid.NewGuid().ToString();
        }
        _nodes.Add(node);
        return Task.FromResult(node);
    }

    public Task<MapNode> UpdateAsync(MapNode node)
    {
        var idx = _nodes.FindIndex(n => n.Id == node.Id);
        if (idx != -1) _nodes[idx] = node;
        return Task.FromResult(node);
    }

    public Task DeleteBySessionIdAsync(string sessionId)
    {
        _nodes.RemoveAll(n => n.SessionId == sessionId);
        return Task.CompletedTask;
    }
}
