using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SheetMusic.Api.Test.Utility;

public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient client, string uri, T body)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, uri)
        {
            Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
        };

        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, string uri, T body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
        };

        return await client.SendAsync(request);
    }
}
