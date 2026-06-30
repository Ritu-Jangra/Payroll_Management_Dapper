namespace Payroll.Business
{
    using Payroll.Contracts;
    using Payroll.Data;
    using Payroll.Domain;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;

    public class AuthService : IAuthService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IPasswordService _passwordService;
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public AuthService(IEmployeeRepository employeeRepository,
                           IPasswordService passwordService,
                           IJwtService jwtService,
                           IRefreshTokenRepository refreshTokenRepository)
        {
            _employeeRepository = employeeRepository;
            _passwordService = passwordService;
            _jwtService = jwtService;
            _refreshTokenRepository = refreshTokenRepository;
        }

        /// <summary>
        /// LoginAsync handles the authentication process for an employee.
        /// It validates the provided credentials, and if they are correct, it generates a JWT access token and a refresh token.
        /// The refresh token is stored securely in the database with an expiry time. This method ensures that only valid users can obtain tokens to access protected resources in the application.
        /// </summary>
        /// <param name="loginRequestDto">Login request data transfer object</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the authentication result containing access and refresh tokens</returns>
        public async Task<AuthResultDto> LoginAsync(LoginRequestDto loginRequestDto,
                                                    CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.GetEmployeeUsernameAsync(loginRequestDto.UserName, cancellationToken);

            // Validate the login request to ensure the employee exists and the password is correct
            await ValidateLoginRequest(loginRequestDto, employee, cancellationToken);

            // Credentials are valid, generate tokens
            return await GenerateAuthTokensAsync(employee!, cancellationToken);
        }

        /// <summary>
        /// LogoutAsync handles the logout process for an employee by revoking the provided refresh token.
        /// </summary>
        /// <param name="refreshToken">The refresh token to revoke</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns true if the logout was successful, false otherwise</returns>
        public async Task<bool> LogoutAsync(string refreshToken,
                                            CancellationToken cancellationToken)
        {
            var hashToken = ComputeHash(refreshToken);

            var existingToken = await _refreshTokenRepository.GetRefreshTokenByHashAsync(hashToken, cancellationToken);

            // Validate the logout request to ensure the token exists and is not revoked or expired
            ValidateLogoutRequest(existingToken);

            // Revoke the refresh token to prevent further use
            return await _refreshTokenRepository.RevokeRefreshTokenAsync(existingToken!, cancellationToken);
        }

        /// <summary>
        /// RefreshTokenAsync handles the process of refreshing an access token using a valid refresh token.
        /// </summary>
        /// <param name="refreshToken">The refresh token to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the authentication result containing new access and refresh tokens</returns>
        /// <exception cref="KeyNotFoundException">Thrown exception when employee is not found</exception>
        public async Task<AuthResultDto> RefreshTokenAsync(string refreshToken,
                                                           CancellationToken cancellationToken)
        {
            var hashToken = ComputeHash(refreshToken);

            var existingToken = await _refreshTokenRepository.GetRefreshTokenByHashAsync(hashToken, cancellationToken);

            // Validate the refresh token to ensure it exists and is not revoked or expired
            ValidateRefreshToken(existingToken);

            var userId = existingToken!.UserId;

            // Get the employee details along with permissions to generate new tokens
            var employee = await _employeeRepository.GetEmployeeWithPermissionsAsync(userId, cancellationToken);

            if (employee != null)
            {
                // Generate new access and refresh tokens for the employee
                var newToken = await GenerateAuthTokensAsync(employee, cancellationToken);

                // Revoke the old refresh token to prevent reuse
                await _refreshTokenRepository.RevokeRefreshTokenAsync(existingToken, cancellationToken);

                // Link the old token to the new token for audit purposes
                existingToken.ReplacedByTokenId = newToken.RefreshTokenId;
                await _refreshTokenRepository.UpdateRefreshTokenAsync(existingToken, cancellationToken);

                return newToken;
            }
            else
            {
                throw new KeyNotFoundException("Employee not found.");
            }
        }

        #region Private Helper Methods

        private async Task<AuthResultDto> GenerateAuthTokensAsync(Employee employee,
                                                                     CancellationToken cancellationToken)
        {
            var accessToken = _jwtService.GenerateAccessToken(employee);
            var refreshToken = _jwtService.GenerateRefreshToken();

            var tokenHash = ComputeHash(refreshToken);

            // Store the refresh token hash in the database with an expiry of 7 days
            var entity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = employee.EmployeeId,
                TokenHash = tokenHash,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            // Save the refresh token to the database for later validation and revocation
            await _refreshTokenRepository.AddRefreshTokenAsync(entity, cancellationToken);

            return new AuthResultDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RefreshTokenId = entity.Id
            };
        }

        private static string ComputeHash(string token)
        {
            var secret = "your-secret-key-from-config";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }

        private async Task ValidateLoginRequest(LoginRequestDto loginRequestDto,
                                                     Employee? employee,
                                                     CancellationToken cancellationToken)
        {
            switch (true)
            {
                case true when employee == null: throw new KeyNotFoundException("Employee not found.");
                case true when !await _passwordService.VerifyPasswordAsync(cancellationToken, employee, employee.PasswordHash, loginRequestDto.Password): throw new UnauthorizedAccessException("Invalid username or password.");
                default: return;
            }
        }

        private static void ValidateRefreshToken(RefreshToken? token)
        {
            switch (true)
            {
                case true when token == null: throw new KeyNotFoundException("Refresh token not found.");
                case true when token.ExpiryDate <= DateTime.UtcNow: throw new UnauthorizedAccessException("This refresh token has expired.");
                case true when token.IsRevoked && token.ReplacedByTokenId != null:
                    throw new UnauthorizedAccessException("Token reuse detected.");
                case true when token.IsRevoked:
                    throw new UnauthorizedAccessException("This refresh token has been revoked.");
                default: return;
            }
        }

        private static void ValidateLogoutRequest(RefreshToken? token)
        {
            switch (true)
            {
                case true when token == null: throw new KeyNotFoundException("Refresh token not found.");
                case true when token.IsRevoked: throw new UnauthorizedAccessException("Token already revoked.");
                case true when token.ExpiryDate <= DateTime.UtcNow: throw new UnauthorizedAccessException("Token expired.");
                default: return;
            }
        }

        #endregion Private Helper Methods
    }
}
