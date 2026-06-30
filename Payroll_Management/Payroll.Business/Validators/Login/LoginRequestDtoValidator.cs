namespace Payroll.Business
{
    using FluentValidation;
    using Payroll.Contracts;

    public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestDtoValidator()
        {
            //Validation rules for UserName
            RuleFor(r => r.UserName)
                .NotEmpty().WithMessage("Username is required")
                .Length(7, 30).WithMessage("Username must be between 7 and 30 characters")
                .Matches("^[a-zA-Z0-9]+$").WithMessage("Username must be alphanumeric characters only without spaces");

            //Validation rules for Password
            RuleFor(r => r.Password)
                .NotEmpty().WithMessage("Password is Required");
        }
    }
}
