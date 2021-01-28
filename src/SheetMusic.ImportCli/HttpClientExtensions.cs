using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace SheetMusic.ImportCli
{
    public static class HttpClientExtensions
    {
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
            var token = JsonConvert.DeserializeAnonymousType(responseString, new { access_token = string.Empty });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.access_token}");

            return client;
        }
    }
}
