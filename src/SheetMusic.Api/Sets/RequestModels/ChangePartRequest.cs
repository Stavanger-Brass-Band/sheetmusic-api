using FluentValidation;

namespace SheetMusic.Api.Sets.RequestModels;

public class ChangePartRequest
{
    /// <summary>
    /// Identifier (guid or name) of the part that replaces the current assignment.
    /// </summary>
    public string PartIdentifier { get; set; } = null!;

    public class Validator : AbstractValidator<ChangePartRequest>
    {
        public Validator()
        {
            RuleFor(request => request.PartIdentifier)
                .NotEmpty()
                .WithMessage("A replacement part identifier (id or name) is required.");
        }
    }
}