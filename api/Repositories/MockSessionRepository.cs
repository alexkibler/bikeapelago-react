using System.Collections.Generic;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Repositories;

public class MockSessionRepository : IGameSessionRepository
{
    private static readonly List<GameSession> _sessions =
    [
        new GameSession {
            Id = "session-1",
            UserId = "test-id",
            ApSeedName = "Adventure Path #42",
            Status = SessionStatus.Active
        }
    ];

    public Task<GameSession?> GetByIdAsync(string id)
    {
        return Task.FromResult(_sessions.Find(s => s.Id == id));
    }

    public Task<IEnumerable<GameSession>> GetByUserIdAsync(string userId)
    {
        return Task.FromResult(_sessions.Where(s => s.UserId == userId));
    }

    public Task<GameSession> CreateAsync(GameSession session)
    {
        session.Id = Guid.NewGuid().ToString();
        _sessions.Add(session);
        return Task.FromResult(session);
    }

    public Task<GameSession> UpdateAsync(GameSession session)
    {
        var idx = _sessions.FindIndex(s => s.Id == session.Id);
        if (idx != -1) _sessions[idx] = session;
        return Task.FromResult(session);
    }

    public Task<bool> DeleteAsync(string id)
    {
        _sessions.RemoveAll(s => s.Id == id);
        return Task.FromResult(true);
    }
}
