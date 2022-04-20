using FluentValidation;

namespace SheetMusic.Api.Controllers.RequestModels;

public class PartRequest
{
    public string Name { get; set; } = null!;

    public int SortOrder { get; set; }

    public bool? Indexable { get; set; }

    public class Validator : AbstractValidator<PartRequest>
    {
        public Validator()
        {
            RuleFor(p => p.Name).NotEmpty().WithMessage("Part name is required");
            RuleFor(p => p.SortOrder)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(99)
                .WithMessage("SortOrder must be a value between 1 and 99. 1 appears before 99 and so on");
            RuleFor(p => p.Indexable).NotNull().WithMessage("Indexable flag is required");
        }
    }
}
