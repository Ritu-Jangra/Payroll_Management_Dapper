namespace Payroll.Data
{
    using Payroll.Domain;

    public interface IRefreshTokenRepository
    {
        Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken);

        Task UpdateRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken);

        Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken);

        Task<bool> RevokeRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken);
    }
}
