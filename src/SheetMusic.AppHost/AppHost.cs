var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithHostPort(32001);

var db = sql.AddDatabase("SheetMusicContext");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume());

var blobs = storage.AddBlobs("AzureStorageConnectionString");

var resendApiKey = builder.AddParameter("resend-api-key", secret: true);
var emailFromAddress = builder.AddParameter("email-from-address");
var emailFrontendBaseUrl = builder.AddParameter("email-frontend-base-url");

builder.AddProject<Projects.SheetMusic_Api>("sheetmusic-api")
    .WithReference(db)
    .WaitFor(db)
    .WithReference(blobs)
    .WaitFor(storage)
    .WithEnvironment("Resend__ApiKey", resendApiKey)
    .WithEnvironment("Email__FromAddress", emailFromAddress)
    .WithEnvironment("Email__FrontendBaseUrl", emailFrontendBaseUrl);

builder.Build().Run();
