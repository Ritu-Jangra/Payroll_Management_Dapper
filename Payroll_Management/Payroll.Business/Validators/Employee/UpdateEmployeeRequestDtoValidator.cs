namespace Payroll.Business
{
    using FluentValidation;
    using Payroll.Contracts;

    public class UpdateEmployeeRequestDtoValidator : AbstractValidator<UpdateEmployeeRequestDto>
    {
        public UpdateEmployeeRequestDtoValidator()
        {
            When(x => x.Role != default, () =>
            {
                RuleFor(x => x.Role)
                .IsInEnum().WithMessage("Role must be either Admin, Manager, or Employee.");
            });

            When(x => x.Salary != 0, () =>
            {
                RuleFor(x => x.Salary)
                .GreaterThan(0).WithMessage("Salary must be greater than zero.")
                .LessThanOrEqualTo(1000000).WithMessage("Salary must be less than or equal to 1,000,000.");
            });

            When(x => x.ManagerId != null, () =>
            {
                RuleFor(x => x.ManagerId)
                .Must(id => id != Guid.Empty).WithMessage("ManagerId must be a valid GUID.");
            });
        }
    }
}
