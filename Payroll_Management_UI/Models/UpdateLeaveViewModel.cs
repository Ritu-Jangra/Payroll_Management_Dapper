namespace Payroll_Management_UI.Models
{
    using System.ComponentModel.DataAnnotations;

    public class UpdateLeaveViewModel
    {
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public required DateTime StartDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public required DateTime EndDate { get; set; }

        public string Reason { get; set; } = default;

        public string Status { get; set; } = default;
    }
}
