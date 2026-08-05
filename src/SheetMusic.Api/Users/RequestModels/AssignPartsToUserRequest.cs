using FluentValidation;
using System;
using System.Collections.Generic;

namespace SheetMusic.Api.Users.RequestModels;

public class AssignPartsToUserRequest
{
    /// <summary>
    /// The identifiers of the parts to assign to the user.
    /// </summary>
    public IReadOnlyList<Guid> PartIds { get; set; } = [];

    public class Validator : AbstractValidator<AssignPartsToUserRequest>
    {
        public Validator()
        {
            RuleFor(r => r.PartIds).NotNull();
            RuleForEach(r => r.PartIds).NotEmpty();
            RuleFor(r => r.PartIds).Must(partIds => partIds.Distinct().Count() == partIds.Count)
                .When(r => r.PartIds is not null)
                .WithMessage("Part identifiers must be unique.");
        }
    }
}