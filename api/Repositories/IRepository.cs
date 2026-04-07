using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(string id);
        Task<User?> GetByUsernameAsync(string username);
        Task<User> CreateAsync(User user, string password);
        Task<User> UpdateAsync(User user);
        Task<(string Token, User User)?> LoginAsync(string username, string password);
        Task<User?> GetCurrentUserAsync(string token);
    }

    public interface IGameSessionRepository
    {
        Task<GameSession?> GetByIdAsync(string id);
        Task<IEnumerable<GameSession>> GetByUserIdAsync(string userId);
        Task<GameSession> CreateAsync(GameSession session);
        Task<GameSession> UpdateAsync(GameSession session);
        Task<bool> DeleteAsync(string id);
    }

    public interface IMapNodeRepository
    {
        Task<IEnumerable<MapNode>> GetBySessionIdAsync(string sessionId);
        Task<MapNode> CreateAsync(MapNode node);
        Task<MapNode> UpdateAsync(MapNode node);
        Task DeleteBySessionIdAsync(string sessionId);
    }
}
