using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Vowlt.Api.Shared.Controllers
{
    public abstract class VowltControllerBase : ControllerBase
    {
        protected Guid GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new UnauthorizedAccessException("User ID not found in claims");
            }

            return Guid.Parse(userIdClaim);
        }
    }

}
