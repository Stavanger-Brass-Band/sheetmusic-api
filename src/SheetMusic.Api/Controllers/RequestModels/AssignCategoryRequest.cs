using FluentValidation;

namespace SheetMusic.Api.Controllers.RequestModels;

public class AssignCategoryRequest
{
    /// <summary>
    /// Identifier (guid or name) of the category to assign
    /// </summary>
    public string CategoryIdentifier { get; set; } = null!;

    public class Validator : AbstractValidator<AssignCategoryRequest>
    {
        public Validator()
        {
            RuleFor(r => r.CategoryIdentifier).NotEmpty().WithMessage("A category identifier (id or name) is required.");
        }
    }
}
