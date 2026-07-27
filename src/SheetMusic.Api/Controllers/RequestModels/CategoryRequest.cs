using FluentValidation;

namespace SheetMusic.Api.Controllers.RequestModels;

public class CategoryRequest
{
    public string Name { get; set; } = null!;

    public bool? Inactive { get; set; }

    public class Validator : AbstractValidator<CategoryRequest>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NotEmpty().WithMessage("Category name is required");
        }
    }
}
