using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SheetMusic.ImportCli;

public static class HttpClientExtensions
{
    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }

    public static async Task<HttpClient> DecorateWithAuthHeaderAsync(this HttpClient client, string username, string password)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent("password"), "grant_type" },
            { new StringContent(username), "username" },
            { new StringContent(password), "password" }
        };

        var response = await client.PostAsync("token", content);
        var responseString = await response.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<TokenResponse>(responseString, JsonDefaults.Options);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token?.AccessToken}");

        return client;
    }
}
