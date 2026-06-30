namespace Payroll.Domain
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Leave
    {
        [Key]
        public Guid LeaveId { get; set; }

        public required ReasonType Reason { get; set; }

        public required DateTime StartDate { get; set; }

        public required DateTime EndDate { get; set; }

        public required StatusType Status { get; set; }

        #region Navigation properties

        // Foreign key to employee to support many-to-one relationship
        public Guid EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        //To know who approved/rejected the leave request (Manager/Admin)
        public Guid? ActionBy { get; set; }

        [ForeignKey("ActionBy")]
        public Employee? Approver { get; set; }

        #endregion Navigation properties
    }
}
