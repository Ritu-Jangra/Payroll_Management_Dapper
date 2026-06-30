namespace Payroll.Business
{
    using FluentValidation;
    using Payroll.Contracts;

    public class UpdateLeaveRequestDtoValidator : AbstractValidator<UpdateLeaveRequestDto>
    {
        public UpdateLeaveRequestDtoValidator()
        {
            When(x => x.StartDate != default && x.EndDate != default, () =>
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
            });

            When(x => x.Reason != default, () =>
            {
                RuleFor(x => x.Reason)
                .IsInEnum().WithMessage("Reason must be a valid enum value.");
            });
        }
    }
}
