using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.CQRS.Query;
using SheetMusic.Api.Database;
using SheetMusic.Api.Errors;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class RemoveCategoryFromSet(string setIdentifier, string categoryIdentifier) : IRequest
{
    public string SetIdentifier { get; } = setIdentifier;
    public string CategoryIdentifier { get; } = categoryIdentifier;

    public class Handler(SheetMusicContext db, IMediator mediator) : IRequestHandler<RemoveCategoryFromSet>
    {
        public async Task Handle(RemoveCategoryFromSet request, CancellationToken cancellationToken)
        {
            var set = await mediator.Send(new GetSet(request.SetIdentifier), cancellationToken);
            if (set is null) throw new NotFoundError(request.SetIdentifier, "Set was not found");

            var category = await mediator.Send(new GetCategory(request.CategoryIdentifier), cancellationToken);
            if (category is null) throw new NotFoundError(request.CategoryIdentifier, "Category was not found");

            var link = await db.SheetMusicCategories
                .FirstOrDefaultAsync(sc => sc.SheetMusicSetId == set.Id && sc.CategoryId == category.Id, cancellationToken: cancellationToken);

            if (link is null)
                throw new NotFoundError($"{set.Title}/{category.Name}", "Category is not assigned to set");

            db.SheetMusicCategories.Remove(link);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
