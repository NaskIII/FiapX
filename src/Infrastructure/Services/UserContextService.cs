using FiapX.Core.Interfaces.Security;
using System.Security.Claims;

namespace FiapX.Infrastructure.Services
{
    public class UserContextService : IUserContext
    {
        public Guid UserId { get; private set; } = Guid.Empty;
        public bool IsAuthenticated { get; private set; } = false;

        public void SetUser(ClaimsPrincipal principal)
        {
            if (principal == null) return;

            IsAuthenticated = principal.Identity?.IsAuthenticated ?? false;

            var claim = principal.FindFirst("id")
                        ?? principal.FindFirst(ClaimTypes.NameIdentifier)
                        ?? principal.FindFirst("sub");

            if (claim != null && Guid.TryParse(claim.Value, out var id))
            {
                UserId = id;
            }
        }
    }
}
