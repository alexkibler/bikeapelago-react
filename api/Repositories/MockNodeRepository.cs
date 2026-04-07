using System.Collections.Generic;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Repositories;

public class MockNodeRepository : IMapNodeRepository
{
    private static readonly List<MapNode> _nodes = [];

    public Task<IEnumerable<MapNode>> GetBySessionIdAsync(Guid sessionId)
    {
        return Task.FromResult(_nodes.Where(n => n.SessionId == sessionId).AsEnumerable());
    }

    public Task<MapNode> CreateAsync(MapNode node)
    {
        if (node.Id == Guid.Empty)
        {
            node.Id = Guid.NewGuid();
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

    public Task DeleteBySessionIdAsync(Guid sessionId)
    {
        _nodes.RemoveAll(n => n.SessionId == sessionId);
        return Task.CompletedTask;
    }
}
