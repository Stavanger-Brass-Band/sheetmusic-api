using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Sets.Queries;

public class GetCategory(string categoryIdentifier) : IRequest<Category?>
{
    public string CategoryIdentifier { get; } = categoryIdentifier;

    public class Handler(SheetMusicContext db) : IRequestHandler<GetCategory, Category?>
    {
        public async Task<Category?> Handle(GetCategory request, CancellationToken cancellationToken)
        {
            if (Guid.TryParse(request.CategoryIdentifier, out var guid))
            {
                return await db.Categories
                    .FirstOrDefaultAsync(c => c.Id == guid, cancellationToken: cancellationToken);
            }

            //ignore casing when comparing on name
            var nameLower = request.CategoryIdentifier.ToLower();

            return await db.Categories
                .FirstOrDefaultAsync(c => c.Name != null && c.Name.ToLower() == nameLower, cancellationToken: cancellationToken);
        }
    }
}
