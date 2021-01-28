using Newtonsoft.Json;
using SheetMusic.Api.Controllers.ViewModels;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SheetMusic.ImportCli
{
    public class SingleZipFileImporter
    {
        private static HttpClient client;

        public SingleZipFileImporter()
        {
            client = new HttpClient { BaseAddress = new Uri("https://sheetmusic-api.azurewebsites.net") };
            client.Timeout = TimeSpan.FromMinutes(20);
        }

        public async Task AuthenticateAsync(string username, string password)
        {
            await client.DecorateWithAuthHeaderAsync(username, password);
        }

        public async Task Import()
        {
            foreach (var file in Directory.GetFiles(@"c:\temp\notearkiv"))
            {
                Console.WriteLine($"Processing file {file}");
                var fileName = Path.GetFileName(file);
                string archiveNumber = fileName.Split("_")[0];

                var set = await client.GetAsync($"sheetmusic/sets/{archiveNumber}/parts");

                var content = JsonConvert.DeserializeObject<ApiSet>(await set.Content.ReadAsStringAsync());

                if (content.Parts == null || content.Parts.Count == 0)
                {
                    Console.WriteLine($"Uploading all parts for set {archiveNumber}");

                    if (await FileUploader.UploadOneFile(file, client, $"sheetmusic/sets/{archiveNumber}"))
                    {
                        Console.WriteLine($"Removing file {file}");
                        File.Delete(file);
                    }
                }
                else
                {
                    Console.WriteLine("Set already has parts, skipping");
                }
            }
        }
    }
}
