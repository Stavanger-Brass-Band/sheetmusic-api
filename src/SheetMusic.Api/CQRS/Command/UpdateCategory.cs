using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Controllers.RequestModels;
using SheetMusic.Api.Database;
using SheetMusic.Api.Errors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class UpdateCategory(Guid categoryId, CategoryRequest request) : IRequest
{
    public Guid CategoryId { get; } = categoryId;
    public CategoryRequest Request { get; } = request;

    public class Handler(SheetMusicContext db) : IRequestHandler<UpdateCategory>
    {
        public async Task Handle(UpdateCategory request, CancellationToken cancellationToken)
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

            if (category is null)
                throw new NotFoundError(request.CategoryId.ToString(), "Category was not found");

            var nameLower = request.Request.Name.ToLower();

            if (await db.Categories.AnyAsync(c => c.Id != request.CategoryId && c.Name != null && c.Name.ToLower() == nameLower, cancellationToken))
                throw new CategoryAlreadyExistsError(request.Request.Name);

            category.Name = request.Request.Name;
            category.Inactive = request.Request.Inactive ?? category.Inactive;

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
