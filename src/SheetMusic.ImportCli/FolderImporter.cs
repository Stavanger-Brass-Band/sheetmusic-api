using Newtonsoft.Json;
using SheetMusic.Api.Controllers.RequestModels;
using SheetMusic.Api.Controllers.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SheetMusic.ImportCli;

public class FolderImporter
{
    public static readonly HttpClient client = new HttpClient()
    {
        BaseAddress = new Uri("https://sheetmusic-api.azurewebsites.net"),
        //BaseAddress = new Uri("https://localhost:5001"),
        Timeout = TimeSpan.FromMinutes(20)
    };

    public async Task AuthenticateAsync(string username, string password)
    {
        await client.DecorateWithAuthHeaderAsync(username, password);
    }

    public async Task Import()
    {
        var rootFolderInfo = new DirectoryInfo(@"c:\temp\notearkiv_ny");
        Console.WriteLine($"Starting import from {rootFolderInfo.FullName}");

        foreach (var folderInfo in rootFolderInfo.GetDirectories())
        {
            Console.WriteLine($@"Processing folder {folderInfo.Name}...");

            var folderName = Regex.Replace(folderInfo.Name, @"(\d\d\d\d\s)", "");
            Console.WriteLine($"Removed numbers, folder name {folderName}");

            var setResponse = await client.GetAsync($"sheetmusic/sets/{folderName}/parts");
            ApiSet set;

            if (setResponse.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Set content for {folderName} not found. Adding.");
                await AddSetAsync(folderName);
                set = new ApiSet { Title = folderInfo.Name };
            }
            else if (!setResponse.IsSuccessStatusCode)
            {
                var responseContent = await setResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Error during set retrieval: [{setResponse.StatusCode}] {responseContent}");
                continue;
            }
            else
            {
                set = JsonConvert.DeserializeObject<ApiSet>(await setResponse.Content.ReadAsStringAsync());
            }

            foreach (var fileInfo in folderInfo.GetFiles())
            {
                Console.WriteLine($"Processing file {fileInfo.Name}...");

                string partName = ExtractPartName(folderName, fileInfo.FullName);
                Console.WriteLine($"Extracted part name [{partName}]");

                if (set.Parts.Any(c => c.Name.Equals(partName, StringComparison.OrdinalIgnoreCase) ||
                                      (c.Aliases != null && c.Aliases.Contains(partName, StringComparison.OrdinalIgnoreCase))))
                {
                    Console.WriteLine($"Part {partName} already exists for set {folderName}. Deleting file.");
                    fileInfo.Delete();
                    continue;
                }

                var endpointAddress = $"sheetmusic/sets/{folderName}/parts/{partName}/content";

                if (await FileUploader.UploadOneFile(fileInfo.FullName, client, endpointAddress) == true)
                {
                    Console.WriteLine($"Part {partName} successfully added, deleting file");
                    fileInfo.Delete();
                }
            }

            if (folderInfo.GetFiles().Count() == 0)
            {
                Console.WriteLine($"All parts processed, removing folder {folderInfo.Name}");
                folderInfo.Delete();
            }
        }
    }

    private static string ExtractPartName(string folderName, string fileName)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        var partName = fileNameWithoutExtension.Replace($"{folderName}_", "").Trim(); //sometimes set name with underscore is included
        partName = partName.Replace(folderName, "").Trim(); //...other times its just folder name
        partName = partName.Replace("(1)", ""); //some files have (1) at the end, for some reason
        partName = partName.Replace("(Fanfare) ", ""); //fanfare not included in title
        partName = Regex.Replace(partName, @"(Fil(\d\d?))", ""); //replace "FilXX" stuff

        return partName.Trim();
    }

    private async Task AddSetAsync(string name)
    {
        var parameters = new SetRequest
        {
            Title = name,
        };

        var content = new StringContent(JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("sheetmusic/sets", content);

        response.EnsureSuccessStatusCode();
    }
}
