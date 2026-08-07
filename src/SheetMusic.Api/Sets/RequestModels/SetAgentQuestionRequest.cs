using FluentValidation;

namespace SheetMusic.Api.Sets.RequestModels;

public sealed class SetAgentQuestionRequest
{
    public string SetName { get; set; } = null!;
    public string Question { get; set; } = null!;

    public sealed class Validator : AbstractValidator<SetAgentQuestionRequest>
    {
        public Validator()
        {
            RuleFor(request => request.SetName).NotEmpty().MaximumLength(500);
            RuleFor(request => request.Question).NotEmpty().MaximumLength(2_000);
        }
    }
}