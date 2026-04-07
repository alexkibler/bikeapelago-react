using System.Collections.Generic;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Services;

namespace Bikeapelago.Api.Repositories;

public class PocketBaseSessionRepository(PocketBaseService pb) : IGameSessionRepository
{
    private readonly PocketBaseService _pb = pb;

    public async Task<GameSession?> GetByIdAsync(string id)
    {
        return await _pb.GetAsync<GameSession>($"collections/game_sessions/records/{id}");
    }

    public async Task<IEnumerable<GameSession>> GetByUserIdAsync(string userId)
    {
        var response = await _pb.GetAsync<PocketBaseListResponse<GameSession>>("collections/game_sessions/records", $"user='{userId}'");
        return response?.Items ?? [];
    }

    public async Task<GameSession> CreateAsync(GameSession session)
    {
        return await _pb.PostAsync<GameSession>("collections/game_sessions/records", session) ?? throw new Exception("Failed to create session");
    }

    public async Task<GameSession> UpdateAsync(GameSession session)
    {
        return await _pb.PatchAsync<GameSession>("collections/game_sessions/records", session.Id, session) ?? throw new Exception("Failed to update session");
    }

    public async Task<bool> DeleteAsync(string id)
    {
        return await _pb.DeleteAsync("collections/game_sessions/records", id);
    }
}
