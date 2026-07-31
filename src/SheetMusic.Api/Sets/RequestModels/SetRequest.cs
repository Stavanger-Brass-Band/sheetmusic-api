using FluentValidation;

namespace SheetMusic.Api.Sets.RequestModels;

public class SetRequest
{
    public int? ArchiveNumber { get; set; }
    public string Title { get; set; } = null!;
    public string? Composer { get; set; } = null!;
    public string? RecordingUrl { get; set; }
    public string? Arranger { get; set; } = null!;
    public string? SoleSellingAgent { get; set; } = null!;
    public string? MissingParts { get; set; } = null!;
    public string? BorrowedFrom { get; set; }

    public class Validator : AbstractValidator<SetRequest>
    {
        public Validator()
        {
            RuleFor(s => s.Title).NotEmpty().WithMessage("Title is required");
            RuleFor(s => s.ArchiveNumber)
                .GreaterThanOrEqualTo(1)
                .When(s => s.ArchiveNumber.HasValue)
                .WithMessage("ArchiveNumber must be a positive number");
        }
    }
}
