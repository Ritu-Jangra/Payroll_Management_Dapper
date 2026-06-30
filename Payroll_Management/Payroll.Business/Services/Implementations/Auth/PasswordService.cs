namespace Payroll.Business
{
    using Microsoft.AspNetCore.Identity;
    using Payroll.Domain;

    public class PasswordService : IPasswordService
    {
        private readonly PasswordHasher<Employee> _hasher = new();

        /// <summary>
        /// Hashes the password of the given user using ASP.NET Core Identity's PasswordHasher.
        /// </summary>
        /// <param name="user">User information</param>
        /// <returns>Returns the hashed password</returns>
        public string HashPassword(Employee user)
        {
            return _hasher.HashPassword(user, user.Password!);
        }

        /// <summary>
        /// Verifies the provided password against the stored hash for the given user using ASP.NET Core Identity's PasswordHasher.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="user">User information</param>
        /// <param name="hash">Stored password hash</param>
        /// <param name="password">Plain text password to verify</param>
        /// <returns>Returns true if the password is valid, false otherwise</returns>
        public async Task<bool> VerifyPasswordAsync(CancellationToken cancellationToken, Employee user, string hash, string password)
        {
            var result = await Task.Run(() => _hasher.VerifyHashedPassword(user, hash, password), cancellationToken);
            return result == PasswordVerificationResult.Success;
        }
    }
}
