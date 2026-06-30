namespace Payroll.Domain
{
    using System.ComponentModel.DataAnnotations;

    public class Role
    {
        [Key]
        public Guid RoleId { get; set; }

        public required RoleType Name { get; set; }

        #region Navigation properties

        // To support one-to-many relationship with Employee
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();

        // To support many-to-many relationship with Permissions
        public List<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        #endregion Navigation properties
    }
}