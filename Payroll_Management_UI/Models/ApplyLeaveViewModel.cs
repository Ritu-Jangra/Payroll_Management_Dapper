namespace Payroll_Management_UI
{
    using System.ComponentModel.DataAnnotations;

    public class ApplyLeaveViewModel
    {
        [DataType(DataType.Date)]
        public required DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public required DateTime EndDate { get; set; }

        public required ReasonType Reason { get; set; }
    }

    public enum ReasonType
    {
        SickLeave,
        CasualLeave,
        MaternityLeave,
        PaternityLeave,
        VacationLeave,
        Other
    }
}
