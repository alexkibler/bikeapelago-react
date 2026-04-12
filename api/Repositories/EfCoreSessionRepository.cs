using Bikeapelago.Api.Data;
using Bikeapelago.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bikeapelago.Api.Repositories;

public class EfCoreSessionRepository(BikeapelagoDbContext context) : IGameSessionRepository
{
    private readonly BikeapelagoDbContext _context = context;

    public async Task<GameSession?> GetByIdAsync(Guid id)
    {
        return await _context.GameSessions.FindAsync(id);
    }

    public async Task<IEnumerable<GameSession>> GetByUserIdAsync(Guid userId)
    {
        return await _context.GameSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<GameSession> CreateAsync(GameSession session)
    {
        session.CreatedAt = DateTime.UtcNow.ToString("O");
        session.UpdatedAt = DateTime.UtcNow.ToString("O");
        
        _context.GameSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task<GameSession> UpdateAsync(GameSession session)
    {
        session.UpdatedAt = DateTime.UtcNow.ToString("O");
        
        _context.GameSessions.Update(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var session = await _context.GameSessions.FindAsync(id);
        if (session != null)
        {
            _context.GameSessions.Remove(session);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<bool> DeleteAllByUserIdAsync(Guid userId)
    {
        var sessions = await _context.GameSessions.Where(s => s.UserId == userId).ToListAsync();
        if (sessions.Any())
        {
            _context.GameSessions.RemoveRange(sessions);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task UpdateReceivedItemsAsync(Guid sessionId, List<long> itemIds)
    {
        await _context.GameSessions
            .Where(s => s.Id == sessionId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.ReceivedItemIds, itemIds)
                .SetProperty(s => s.UpdatedAt, DateTime.UtcNow.ToString("O"))
            );
    }
}
