namespace Payroll_Management_UI
{
    public class EmployeeViewModel
    {
        public required string UserName { get; set; }

        public required string FirstName { get; set; }

        public string? LastName { get; set; }

        public required string Email { get; set; }

        public string Department { get; set; }

        public decimal Salary { get; set; }

        public string Role { get; set; }

        public string? ManagerName { get; set; }
    }
}
