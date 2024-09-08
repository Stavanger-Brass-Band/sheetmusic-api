using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SheetMusic.Api.Test.Utility;

public static class FileUploader
{
    public static async Task<bool> UploadOneFile(string path, HttpClient client, string endpoint)
    {
        using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        var content = new StreamContent(fileStream);
        content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = "file", FileName = Path.GetFileName(path) };
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var formData = new MultipartFormDataContent();
        formData.Add(content);
        var response = await client.PostAsync(endpoint, formData);

        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error during upload: [{response.StatusCode}] {responseContent}");
            return false;
        }
        else
        {
            Console.WriteLine("Completed successfully");
            return true;
        }
    }

    public static async Task UploadFromStream(Stream stream, HttpClient client, string endpoint)
    {
        var content = new StreamContent(stream);
        content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = "file", FileName = Path.GetTempFileName() };
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var formData = new MultipartFormDataContent();
        formData.Add(content);
        var response = await client.PostAsync(endpoint, formData);

        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error during upload: [{response.StatusCode}] {responseContent}");
        }
        else
        {
            Console.WriteLine("Completed successfully");
        }
    }
}
