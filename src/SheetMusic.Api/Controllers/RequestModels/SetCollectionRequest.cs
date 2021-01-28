using FluentValidation;
using System.Collections.Generic;

namespace SheetMusic.Api.Controllers.RequestModels
{
    public class SetCollectionRequest
    {
        public List<string> SetIdentifiers { get; set; } = null!;

        public class Validator : AbstractValidator<SetCollectionRequest>
        {
            public Validator()
            {
                RuleFor(s => s.SetIdentifiers).NotEmpty().WithMessage("Please provide at least one set identifier");
            }
        }
    }
}
