using FluentValidation;
using Vowlt.Api.Features.Search.DTOs;

namespace Vowlt.Api.Features.Search.Validators;

public class SearchRequestValidator : AbstractValidator<SearchRequest>
{
    public SearchRequestValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .WithMessage("Search query is required.")
            .MinimumLength(2)
            .WithMessage("Search query must be at least 2 characters.")
            .MaximumLength(500)
            .WithMessage("Search query must not exceed 500 characters.");

        RuleFor(x => x.Limit)
            .GreaterThan(0)
            .WithMessage("Limit must be greater than 0.")
            .LessThanOrEqualTo(100)
            .WithMessage("Limit must not exceed 100.");

        RuleFor(x => x.MinimumScore)
            .InclusiveBetween(0.0, 1.0)
            .WithMessage("Minimum score must be between 0 and 1.");

        RuleFor(x => x.Mode)
            .IsInEnum()
            .When(x => x.Mode.HasValue)
            .WithMessage("Invalid search mode. Must be Vector, Keyword, or Hybrid.");
    }
}
