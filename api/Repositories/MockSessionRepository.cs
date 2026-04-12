using System.Collections.Generic;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Repositories;

public class MockSessionRepository : IGameSessionRepository
{
    private static readonly List<GameSession> _sessions =
    [
        new GameSession {
            Id = Guid.Empty,
            UserId = Guid.Empty,
            ApSeedName = "Adventure Path #42",
            Status = SessionStatus.Active
        }
    ];

    public Task<GameSession?> GetByIdAsync(Guid id)
    {
        return Task.FromResult<GameSession?>(_sessions.Find(s => s.Id == id));
    }

    public Task<IEnumerable<GameSession>> GetByUserIdAsync(Guid userId)
    {
        return Task.FromResult(_sessions.Where(s => s.UserId == userId).AsEnumerable());
    }

    public Task<GameSession> CreateAsync(GameSession session)
    {
        session.Id = Guid.NewGuid();
        _sessions.Add(session);
        return Task.FromResult(session);
    }

    public Task<GameSession> UpdateAsync(GameSession session)
    {
        var idx = _sessions.FindIndex(s => s.Id == session.Id);
        if (idx != -1) _sessions[idx] = session;
        return Task.FromResult(session);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        _sessions.RemoveAll(s => s.Id == id);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAllByUserIdAsync(Guid userId)
    {
        _sessions.RemoveAll(s => s.UserId == userId);
        return Task.FromResult(true);
    }

    public Task UpdateReceivedItemsAsync(Guid sessionId, List<long> itemIds)
    {
        var session = _sessions.Find(s => s.Id == sessionId);
        if (session != null)
        {
            session.ReceivedItemIds = itemIds;
            session.UpdatedAt = DateTime.UtcNow.ToString("O");
        }
        return Task.CompletedTask;
    }
}
