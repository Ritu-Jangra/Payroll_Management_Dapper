namespace Payroll.Contracts
{
    using Payroll.Domain;

    public class AddEmployeeRequestDto
    {
        public required string FirstName { get; set; }

        public string? LastName { get; set; }

        public required string UserName { get; set; }

        public required string Email { get; set; }

        public required string Password { get; set; }

        public required DepartmentType Department { get; set; }

        public required decimal Salary { get; set; }

        public required RoleType Role { get; set; }

        public Guid? ManagerId { get; set; }
    }
}
