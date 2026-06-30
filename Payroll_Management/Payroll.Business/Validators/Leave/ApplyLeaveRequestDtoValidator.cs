namespace Payroll.Business
{
    using FluentValidation;
    using Payroll.Contracts;

    public class ApplyLeaveRequestDtoValidator : AbstractValidator<ApplyLeaveRequestDto>
    {
        public ApplyLeaveRequestDtoValidator()
        {
            var today = DateTime.UtcNow.Date;
            var maxDate = today.AddDays(90);

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Start date is required.")
                .Must(d => d.Date >= today).WithMessage("Start date cannot be in the past.")
                .Must(d => d.Date <= maxDate).WithMessage("Start date must be within the next 90 days.");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("End date is required.")
                .Must(d => d.Date >= today).WithMessage("End date cannot be in the past.")
                .Must(d => d.Date <= maxDate).WithMessage("End date must be within the next 90 days.");

            RuleFor(x => x)
                .Must(x => x.EndDate.Date >= x.StartDate.Date).WithMessage("End date must be on or after the start date.");

            RuleFor(x => x.Reason)
                .IsInEnum().WithMessage("Reason must be a valid reason type.");
        }
    }
}
