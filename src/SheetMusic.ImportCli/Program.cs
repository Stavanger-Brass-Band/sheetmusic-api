using System;
using System.Threading.Tasks;

namespace SheetMusic.ImportCli;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Importer console app initializing...");
        //await Task.Delay(7000); //wait for localhost to start

        Console.WriteLine("Please type user name");
        string username = Console.ReadLine();
        Console.WriteLine("Please type password");
        string password = Console.ReadLine();

        var zipImporter = new SingleZipFileImporter();
        await zipImporter.AuthenticateAsync(username, password);
        await zipImporter.Import();

        var folderImporter = new FolderImporter();
        await folderImporter.AuthenticateAsync(username, password);
        await folderImporter.Import();

        Console.WriteLine("Completed, press any key to exit");
        Console.Read();
    }
}
