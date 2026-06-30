namespace Payroll.Contracts
{
    using Payroll.Domain;

    public class ApplyLeaveRequestDto
    {
        public required DateTime StartDate { get; set; }

        public required DateTime EndDate { get; set; }

        public required ReasonType Reason { get; set; }
    }
}
