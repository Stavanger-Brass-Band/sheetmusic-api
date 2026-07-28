using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Sets.Errors;
using SheetMusic.Api.Sets.Queries;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Sets.Commands;

public class AssignCategoryToSet(string setIdentifier, string categoryIdentifier) : IRequest<SheetMusicCategory>
{
    public string SetIdentifier { get; } = setIdentifier;
    public string CategoryIdentifier { get; } = categoryIdentifier;

    public class Handler(SheetMusicContext db, IMediator mediator) : IRequestHandler<AssignCategoryToSet, SheetMusicCategory>
    {
        public async Task<SheetMusicCategory> Handle(AssignCategoryToSet request, CancellationToken cancellationToken)
        {
            var set = await mediator.Send(new GetSet(request.SetIdentifier), cancellationToken);
            if (set is null) throw new NotFoundError(request.SetIdentifier, "Set was not found");

            var category = await mediator.Send(new GetCategory(request.CategoryIdentifier), cancellationToken);
            if (category is null) throw new NotFoundError(request.CategoryIdentifier, "Category was not found");

            var existing = await db.SheetMusicCategories
                .FirstOrDefaultAsync(sc => sc.SheetMusicSetId == set.Id && sc.CategoryId == category.Id, cancellationToken: cancellationToken);

            if (existing is not null)
                throw new CategoryAlreadyAssignedError(set.Title, category.Name ?? category.Id.ToString());

            var link = new SheetMusicCategory
            {
                Id = Guid.NewGuid(),
                SheetMusicSetId = set.Id,
                CategoryId = category.Id
            };

            db.SheetMusicCategories.Add(link);
            await db.SaveChangesAsync(cancellationToken);

            link.Category = category;

            return link;
        }
    }
}
