using System.Net.Http.Json;
using System.Text.Json;

namespace SheetMusic.Api.Sets.Services;

public sealed class MetadataAgentClient(HttpClient httpClient, IConfiguration configuration, ILogger<MetadataAgentClient> logger) : IMetadataAgentClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string sharedSecret = configuration["Agent:SharedSecret"] ?? string.Empty;

    public async Task<string?> ClassifyPartAsync(string fileName, IReadOnlyList<string> candidateNames, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/classify/part")
            {
                Content = JsonContent.Create(new { FileName = fileName, CandidateNames = candidateNames }, options: JsonOptions),
            };
            AddSecret(request);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Metadata agent returned {StatusCode} while matching part {FileName}", response.StatusCode, fileName);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<PartClassificationResponse>(JsonOptions, cancellationToken);
            return result?.Match;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Metadata agent failed while matching part {FileName}; treating it as unresolved", fileName);
            return null;
        }
    }

    public async Task<IReadOnlyList<string>> ClassifyCategoryAsync(
        string? title,
        string? composer,
        string? arranger,
        IReadOnlyList<string> projectNames,
        IReadOnlyList<string> candidateCategories,
        IReadOnlyList<CategoryExample> examples,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/classify/category")
            {
                Content = JsonContent.Create(new
                {
                    Title = title,
                    Composer = composer,
                    Arranger = arranger,
                    ProjectNames = projectNames,
                    CandidateCategories = candidateCategories,
                    Examples = examples,
                }, options: JsonOptions),
            };
            AddSecret(request);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Metadata agent returned {StatusCode} while classifying set {Title}", response.StatusCode, title);
                return [];
            }

            var result = await response.Content.ReadFromJsonAsync<CategoryClassificationResponse>(JsonOptions, cancellationToken);
            return result?.Categories ?? [];
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Metadata agent failed while classifying set {Title}; treating it as abstention", title);
            return [];
        }
    }

    private void AddSecret(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(sharedSecret))
            request.Headers.Add("X-Agent-Secret", sharedSecret);
    }

    private sealed record PartClassificationResponse(string? Match);
    private sealed record CategoryClassificationResponse(IReadOnlyList<string> Categories);
}
