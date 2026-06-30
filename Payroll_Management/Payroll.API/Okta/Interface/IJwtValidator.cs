using System.IdentityModel.Tokens.Jwt;

namespace Payroll.API.Okta.Interface
{
    public interface IJwtValidator
    {
        Task<JwtSecurityToken> ValidateToken(string token, CancellationToken ct = default(CancellationToken));
    }
}
