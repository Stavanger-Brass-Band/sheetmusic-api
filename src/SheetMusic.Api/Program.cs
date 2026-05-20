using Asp.Versioning.ApiExplorer;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Configuration;
using SheetMusic.Api.Database;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Repositories;
using SheetMusic.Api.Search;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<SheetMusicContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SheetMusicContext")));

builder.Services.Configure<FormOptions>(x =>
{
    x.ValueLengthLimit = int.MaxValue;
    x.MultipartBodyLengthLimit = int.MaxValue;
});

builder.Services.AddSheetMusicSecurity(builder.Configuration);
builder.Services.AddSheetMusicVersioning();
builder.Services.AddSheetMusicSwagger();

builder.Services.AddSingleton<IBlobClient, BlobClient>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddSingleton<IIndexAdminService, IndexAdminService>();

builder.Services.AddMediatR(config => config.RegisterServicesFromAssemblyContaining<Program>());

builder.Services.AddHealthChecks();
builder.Services.AddMemoryCache();

builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
    db.Database.Migrate();

    if (app.Environment.IsDevelopment())
    {
        await DatabaseSeeder.SeedDevelopmentDataAsync(scope.ServiceProvider);
    }
}

app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.UseCors("AllowMember");

var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DisplayRequestDuration();

    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
    }
    options.OAuthAppName("SheetMusic API");
});

app.MapControllers();
app.MapHealthChecks("/health");

app.MapDefaultEndpoints();

app.Run();
