namespace Payroll.Business
{
    using Payroll.Domain;

    public interface IJwtService
    {
        string GenerateAccessToken(Employee user);

        string GenerateRefreshToken();
    }
}
