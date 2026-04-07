using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Services;
using System.Text.Json.Serialization;

namespace Bikeapelago.Api.Repositories;

public class PocketBaseUserRepository(PocketBaseService pb) : IUserRepository
{
    private readonly PocketBaseService _pb = pb;

    public async Task<User?> GetByIdAsync(string id)
    {
        return await _pb.GetAsync<User>($"collections/users/records/{id}");
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        var response = await _pb.GetAsync<PocketBaseListResponse<User>>("collections/users/records", $"username='{username}'");
        return response?.Items.FirstOrDefault();
    }

    public async Task<User> CreateAsync(User user, string password)
    {
        var data = new {
            username = user.Username,
            password = password,
            passwordConfirm = password,
            name = user.Name,
            weight = user.Weight
        };
        return await _pb.PostAsync<User>("collections/users/records", data) ?? throw new Exception("Failed to create user");
    }

    public async Task<User> UpdateAsync(User user)
    {
        return await _pb.PatchAsync<User>("collections/users/records", user.Id, user) ?? throw new Exception("Failed to update user");
    }

    public async Task<(string Token, User User)?> LoginAsync(string username, string password)
    {
        var data = new { identity = username, password = password };
        var response = await _pb.PostAsync<PocketBaseAuthResponse>("collections/users/auth-with-password", data);

        if (response != null)
        {
            _pb.Token = response.Token;
            return (response.Token, response.Record);
        }
        return null;
    }

    public async Task<User?> GetCurrentUserAsync(string token)
    {
        // PocketBase auth-refresh validates the token and returns the current user
        _pb.Token = token;
        var response = await _pb.PostAsync<PocketBaseAuthResponse>("collections/users/auth-refresh", new {});
        return response?.Record;
    }

    private class PocketBaseAuthResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
        [JsonPropertyName("record")]
        public User Record { get; set; } = new();
    }
}
