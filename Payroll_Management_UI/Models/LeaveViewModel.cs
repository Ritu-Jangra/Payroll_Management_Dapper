namespace Payroll_Management_UI
{
    using System.ComponentModel.DataAnnotations;

    public class LeaveViewModel
    {
        public Guid LeaveId { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime StartDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime EndDate { get; set; }

        public string Reason { get; set; }

        public string Status { get; set; }

        public required string EmployeeName { get; set; }

        public string? ActionByName { get; set; }
    }
}
