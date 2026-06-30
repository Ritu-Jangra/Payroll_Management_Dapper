namespace Payroll.Contracts
{
    using Payroll.Domain;

    public class UpdateEmployeeRequestDto
    {
        public decimal Salary { get; set; } = 0;

        public Guid? ManagerId { get; set; }

        public RoleType Role { get; set; } = default;
    }
}
