namespace Payroll.Contracts
{
    using Payroll.Domain;

    public class LeaveResponseDto
    {
        public Guid LeaveId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public ReasonType Reason { get; set; }

        public StatusType Status { get; set; }

        public required string EmployeeName { get; set; }

        public string? ActionByName { get; set; }
    }
}
