namespace Payroll.Contracts
{
    using Payroll.Domain;

    public class EmployeeResponseDto
    {
        public Guid EmployeeId { get; set; }

        public required string UserName { get; set; }

        public required string FirstName { get; set; }

        public string? LastName { get; set; }

        public required string Email { get; set; }

        public DepartmentType Department { get; set; }

        public decimal Salary { get; set; }

        public RoleType Role { get; set; }

        public string? ManagerName { get; set; }
    }
}
