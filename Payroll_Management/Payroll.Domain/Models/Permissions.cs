namespace Payroll.Domain
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Permissions
    {
        [Key]
        public required int PermissionId { get; set; }

        public required PermissionType PermissionName { get; set; }

        #region Navigation properties

        // To support many-to-many relationship with Role
        public List<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        #endregion Navigation properties
    }
}
