using FluentValidation;
using Vowlt.Api.Features.Search.DTOs;

namespace Vowlt.Api.Features.Search.Validators;

public class SearchRequestValidator : AbstractValidator<SearchRequest>
{
    public SearchRequestValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(500);

        RuleFor(x => x.Limit)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);

        RuleFor(x => x.MinimumScore)
            .InclusiveBetween(0.0, 1.0);

        RuleFor(x => x.FromDate)
            .LessThan(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("FromDate must be before ToDate");
    }
}
