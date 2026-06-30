namespace Payroll.Domain
{
    using System.ComponentModel.DataAnnotations.Schema;

    public class RolePermission
    {
        public Guid RoleId { get; set; }

        public int PermissionId { get; set; }

        #region Navigation properties

        [ForeignKey("RoleId")]
        public Role? Role { get; set; }

        [ForeignKey("PermissionId")]
        public Permissions? Permission { get; set; }

        #endregion Navigation properties
    }
}
