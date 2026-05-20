var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var db = sql.AddDatabase("SheetMusicContext");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume());

var blobs = storage.AddBlobs("AzureStorageConnectionString");

builder.AddProject<Projects.SheetMusic_Api>("sheetmusic-api")
    .WithReference(db)
    .WaitFor(db)
    .WithReference(blobs)
    .WaitFor(storage);

builder.Build().Run();
