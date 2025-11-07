using FluentValidation;
using Vowlt.Api.Features.Auth.DTOs;

namespace Vowlt.Api.Features.Auth.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}

