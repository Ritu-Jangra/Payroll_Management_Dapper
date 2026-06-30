namespace Payroll.Contracts
{
    using Payroll.Domain;

    public class UpdateLeaveRequestDto
    {
        public required DateTime StartDate { get; set; }

        public required DateTime EndDate { get; set; }

        public ReasonType Reason { get; set; } = default;

        public StatusType Status { get; set; } = default;
    }
}
