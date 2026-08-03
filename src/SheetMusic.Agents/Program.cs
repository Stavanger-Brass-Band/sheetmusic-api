using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Agents;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureChatCompletionsClient("chat").AddChatClient();
builder.Services.AddDbContext<AgentDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("SheetMusicContext"), sqlOptions =>
		sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));
builder.Services.AddSingleton<MetadataAgent>();
builder.Services.AddScoped<CategoryBackfill>();

var app = builder.Build();

if (args.Any(argument => string.Equals(argument, "--backfill", StringComparison.OrdinalIgnoreCase)))
{
	using var scope = app.Services.CreateScope();
	var backfill = scope.ServiceProvider.GetRequiredService<CategoryBackfill>();
	var dryRun = args.Any(argument => string.Equals(argument, "--dry-run", StringComparison.OrdinalIgnoreCase));
	var limit = 100;
	var limitIndex = Array.FindIndex(args, argument => string.Equals(argument, "--limit", StringComparison.OrdinalIgnoreCase));
	if (limitIndex >= 0)
	{
		if (limitIndex + 1 >= args.Length || !int.TryParse(args[limitIndex + 1], out limit) || limit <= 0)
			throw new ArgumentException("--limit must be followed by a positive integer.", "args");
	}
	await backfill.RunAsync(limit, dryRun, CancellationToken.None);
	return;
}

var sharedSecret = app.Configuration["Agent:SharedSecret"];
if (string.IsNullOrWhiteSpace(sharedSecret))
	throw new InvalidOperationException("Agent:SharedSecret must be configured.");

app.UseWhen(
	context => context.Request.Path.StartsWithSegments("/classify"),
	branch => branch.Use(async (context, next) =>
	{
		if (!context.Request.Headers.TryGetValue("X-Agent-Secret", out var suppliedSecret) ||
			!string.Equals(suppliedSecret, sharedSecret, StringComparison.Ordinal))
		{
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			return;
		}

		await next(context);
	}));

app.MapPost("/classify/part", async (PartClassificationRequest request, MetadataAgent agent, CancellationToken cancellationToken) =>
	Results.Ok(await agent.ClassifyPartAsync(request, cancellationToken)));

app.MapPost("/classify/category", async (CategoryClassificationRequest request, MetadataAgent agent, CancellationToken cancellationToken) =>
	Results.Ok(await agent.ClassifyCategoryAsync(request, cancellationToken)));

app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new HealthCheckOptions
{
	Predicate = check => check.Tags.Contains("live"),
});
app.MapDefaultEndpoints();


app.Run();

public partial class Program;
