using Microsoft.AspNetCore.Identity;

namespace Vowlt.Api.Features.Auth.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}

