using FluentValidation;
using Vowlt.Api.Features.Bookmarks.DTOs;

namespace Vowlt.Api.Features.Bookmarks.Validators;

public class UpdateBookmarkRequestValidator : AbstractValidator<UpdateBookmarkRequest>
{
    public UpdateBookmarkRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(500)
            .When(x => x.Title != null);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description != null);

        RuleFor(x => x.Notes)
            .MaximumLength(10000)
            .When(x => x.Notes != null);
    }
}
