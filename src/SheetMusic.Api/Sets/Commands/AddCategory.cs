using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Sets.Errors;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Sets.Commands;

public class AddCategory(string name, bool inactive) : IRequest<Category>
{
    public string Name { get; } = name;
    public bool Inactive { get; } = inactive;

    public class Handler(SheetMusicContext db) : IRequestHandler<AddCategory, Category>
    {
        public async Task<Category> Handle(AddCategory request, CancellationToken cancellationToken)
        {
            var nameLower = request.Name.ToLower();

            if (await db.Categories.AnyAsync(c => c.Name != null && c.Name.ToLower() == nameLower, cancellationToken))
                throw new CategoryAlreadyExistsError(request.Name);

            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Inactive = request.Inactive
            };

            db.Categories.Add(category);
            await db.SaveChangesAsync(cancellationToken);

            return category;
        }
    }
}
