namespace Payroll.Domain
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Employee
    {
        [Key]
        public Guid EmployeeId { get; set; }

        public required string FirstName { get; set; }

        public string? LastName { get; set; }

        public required string UserName { get; set; }

        public required string Email { get; set; }

        [NotMapped] // This property is for input only and should not be mapped to the database mainly used by Admins when adding new employee
        public string? Password { get; set; }

        public required string PasswordHash { get; set; }

        public required DepartmentType Department { get; set; }

        public required decimal Salary { get; set; }

        #region Manager-Employee relationship (self-referencing)

        [ForeignKey("Manager")]
        public Guid? ManagerId { get; set; }    // Nullable for top-level admins

        // Navigation to manager
        public Employee? Manager { get; set; }

        // Employees reporting to this manager
        public ICollection<Employee> TeamMembers { get; set; } = new List<Employee>();

        #endregion Manager-Employee relationship (self-referencing)

        #region Navigation properties

        // Foreign key to Role to support many-to-one relationship
        public Guid RoleId { get; set; }

        [ForeignKey("RoleId")]
        public Role? Role { get; set; }

        // Navigation property for one-to-many relationship with Leave
        public ICollection<Leave> Leaves { get; set; } = new List<Leave>();

        #endregion Navigation properties
    }
}
