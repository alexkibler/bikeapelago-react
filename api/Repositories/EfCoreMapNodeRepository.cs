using Bikeapelago.Api.Data;
using Bikeapelago.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bikeapelago.Api.Repositories;

public class EfCoreMapNodeRepository(BikeapelagoDbContext context) : IMapNodeRepository
{
    private readonly BikeapelagoDbContext _context = context;

    public async Task<IEnumerable<MapNode>> GetBySessionIdAsync(Guid sessionId)
    {
        return await _context.MapNodes
            .Where(m => m.SessionId == sessionId)
            .ToListAsync();
    }

    public async Task<MapNode?> GetByIdAsync(Guid id)
    {
        return await _context.MapNodes.FindAsync(id);
    }

    public async Task<MapNode> CreateAsync(MapNode node)
    {
        _context.MapNodes.Add(node);
        await _context.SaveChangesAsync();
        return node;
    }

    public async Task<MapNode> UpdateAsync(MapNode node)
    {
        _context.MapNodes.Update(node);
        await _context.SaveChangesAsync();
        return node;
    }

    public async Task DeleteBySessionIdAsync(Guid sessionId)
    {
        var nodes = await _context.MapNodes.Where(m => m.SessionId == sessionId).ToListAsync();
        if (nodes.Any())
        {
            _context.MapNodes.RemoveRange(nodes);
            await _context.SaveChangesAsync();
        }
    }
}
