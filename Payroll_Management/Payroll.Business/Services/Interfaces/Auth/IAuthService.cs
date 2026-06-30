namespace Payroll.Business
{
    using Payroll.Contracts;

    public interface IAuthService
    {
        Task<AuthResultDto> LoginAsync(LoginRequestDto loginRequestDto, CancellationToken cancellationToken);

        Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken);

        Task<AuthResultDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    }
}
