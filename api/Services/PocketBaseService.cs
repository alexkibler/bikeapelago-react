using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace Bikeapelago.Api.Services
{
    public class PocketBaseService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string? _token;

        public PocketBaseService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["PocketBase:BaseUrl"] ?? "http://127.0.0.1:8090";
            _httpContextAccessor = httpContextAccessor;
        }

        public string BaseUrl => _baseUrl;
        public string? Token { get => _token; set => _token = value; }

        public async Task<T?> GetAsync<T>(string endpoint, string? filter = null, int perPage = 100)
        {
            var url = $"{_baseUrl}/api/{endpoint}?perPage={perPage}";
            if (filter != null)
            {
                url += $"&filter=({Uri.EscapeDataString(filter)})";
            }

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddAuthHeader(request);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content);
        }

        public async Task<T?> PostAsync<T>(string endpoint, object data)
        {
            var url = $"{_baseUrl}/api/{endpoint}";
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            AddAuthHeader(request);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseContent);
        }

        public async Task<T?> PatchAsync<T>(string endpoint, string id, object data)
        {
            var url = $"{_baseUrl}/api/{endpoint}/{id}";
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
            AddAuthHeader(request);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseContent);
        }

        public async Task<bool> DeleteAsync(string endpoint, string id)
        {
            var url = $"{_baseUrl}/api/{endpoint}/{id}";
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            AddAuthHeader(request);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        private void AddAuthHeader(HttpRequestMessage request)
        {
            if (!string.IsNullOrEmpty(_token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
            }
            else
            {
                var authHeader = _httpContextAccessor.HttpContext?.Request?.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
            }
        }
    }
}
