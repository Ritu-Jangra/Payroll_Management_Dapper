namespace Payroll.Business
{
    using Payroll.Domain;

    public interface IPasswordService
    {
        string HashPassword(Employee user);

        Task<bool> VerifyPasswordAsync(CancellationToken cancellationToken, Employee user, string hash, string password);
    }
}
