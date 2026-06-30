namespace Payroll.Business
{
    using Microsoft.AspNetCore.Http;
    using Payroll.Domain;
    using System.Security.Claims;

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// UserId retrieves the unique identifier of the currently authenticated user from the HTTP context.
        /// It accesses the user's claims to find the claim with the type NameIdentifier, which typically contains the user's ID.
        /// The value of this claim is then parsed as a Guid and returned. This allows other parts of the application to easily access the current user's ID for authorization and other purposes.
        /// </summary>
        public Guid UserId => Guid.Parse(_httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        /// <summary>
        /// UserName retrieves the username of the currently authenticated user from the HTTP context.
        /// </summary>
        public string? UserName => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;

        /// <summary>
        /// Role retrieves the role of the currently authenticated user from the HTTP context.
        /// </summary>
        public RoleType Role => Enum.Parse<RoleType>(_httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value!);
    }
}
