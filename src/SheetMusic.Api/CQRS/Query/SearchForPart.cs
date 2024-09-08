using MediatR;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Search;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Queries;

public class SearchForPart(params string[] searchFragments) : IRequest<MusicPart?>
{
    public string[] SearchFragments { get; } = searchFragments;

    public class Handler(IIndexAdminService indexAdminService, SheetMusicContext db, ILogger<SearchForPart.Handler> logger) : IRequestHandler<SearchForPart, MusicPart?>
    {
        public async Task<MusicPart?> Handle(SearchForPart request, CancellationToken cancellationToken)
        {
            var client = indexAdminService.GetQueryClient<PartIndex>();

            try
            {
                var searchTerm = string.Join(" AND ", request.SearchFragments)
                    .Replace("[", "l")
                    .Replace("]", "l")
                    .Replace("'", "")
                    .Replace("`","");

                var searchResult = await client.Documents.SearchAsync<PartIndex>($"{searchTerm}~", new SearchParameters { QueryType = QueryType.Full, SearchMode = SearchMode.All });
                var foundPart = searchResult.Results.FirstOrDefault();

                logger.LogInformation($"Search for '{searchTerm}' resulted in '{foundPart?.Document.PartName ?? string.Empty}'");

                if (foundPart == null) return null;

                var foundPartId = Guid.Parse(foundPart.Document.Id);

                var part = await db.MusicParts
                    .Include(a => a.Aliases)
                    .FirstOrDefaultAsync(p => p.Id == foundPartId, cancellationToken: cancellationToken);

                return part;
            }
            catch (Exception)
            {
                //TODO: Log error
                return null; //part not found 
            }
        }
    }
}
