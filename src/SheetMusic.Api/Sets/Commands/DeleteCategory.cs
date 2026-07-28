using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Sets.Errors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Sets.Commands;

public class DeleteCategory(Guid categoryId) : IRequest
{
    public Guid CategoryId { get; } = categoryId;

    public class Handler(SheetMusicContext db) : IRequestHandler<DeleteCategory>
    {
        public async Task Handle(DeleteCategory request, CancellationToken cancellationToken)
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

            if (category is null)
                throw new NotFoundError(request.CategoryId.ToString(), "Category was not found");

            var isInUse = await db.SheetMusicCategories.AnyAsync(sc => sc.CategoryId == request.CategoryId, cancellationToken);

            if (isInUse)
                throw new CategoryInUseError(category.Name ?? category.Id.ToString());

            db.Categories.Remove(category);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
