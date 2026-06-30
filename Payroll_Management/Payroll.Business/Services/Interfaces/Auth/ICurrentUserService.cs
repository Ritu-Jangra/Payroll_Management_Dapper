using Payroll.Domain;

namespace Payroll.Business
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }

        string? UserName { get; }

        RoleType Role { get; }
    }
}
