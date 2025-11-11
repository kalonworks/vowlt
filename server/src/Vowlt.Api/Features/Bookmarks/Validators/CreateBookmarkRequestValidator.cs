
using FluentValidation;
using Vowlt.Api.Features.Bookmarks.DTOs;

namespace Vowlt.Api.Features.Bookmarks.Validators;

public class CreateBookmarkRequestValidator : AbstractValidator<CreateBookmarkRequest>
{
    public CreateBookmarkRequestValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(BeAValidUrl).WithMessage("URL must be a valid HTTP or HTTPS URL");

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

    private static bool BeAValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
