using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameOrEmailAsync(string identity);
    Task<User> CreateAsync(User user, string password);
    Task<User> UpdateAsync(User user);
    Task<(string Token, User User)?> LoginAsync(string identity, string password);
    Task<User?> GetCurrentUserAsync(string token);
}

public interface IGameSessionRepository
{
    Task<GameSession?> GetByIdAsync(Guid id);
    Task<IEnumerable<GameSession>> GetByUserIdAsync(Guid userId);
    Task<GameSession> CreateAsync(GameSession session);
    Task<GameSession> UpdateAsync(GameSession session);
    Task<bool> DeleteAsync(Guid id);
}

public interface IMapNodeRepository
{
    Task<IEnumerable<MapNode>> GetBySessionIdAsync(Guid sessionId);
    Task<MapNode?> GetByIdAsync(Guid id);
    Task<MapNode> CreateAsync(MapNode node);
    Task CreateRangeAsync(IEnumerable<MapNode> nodes);
    Task<MapNode> UpdateAsync(MapNode node);
    Task DeleteBySessionIdAsync(Guid sessionId);
}
