namespace Payroll.Business
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.IdentityModel.Tokens;
    using Payroll.Domain;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Generates a JWT access token for the given user, including claims for the user's ID, username, role, and permissions.
        /// </summary>
        /// <param name="user">User information</param>
        /// <returns>Returns the generated JWT access token</returns>
        public string GenerateAccessToken(Employee user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.EmployeeId.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role!.Name.ToString())  
            };

            // Add permissions based on the user's role
            foreach (var permission in user.Role.RolePermissions)
            {
                claims.Add(new Claim("Permission", Enum.GetName(permission.Permission!.PermissionName)!));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(_config["Jwt:ExpiryMinutes"])
                ),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Generates a secure random refresh token as a base64 string.
        /// This token is used to obtain new access tokens without requiring the user to re-authenticate, and should be stored securely in the database with an associated expiry time.
        /// </summary>
        /// <returns>Returns the generated refresh token</returns>
        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }
    }
}
