namespace Payroll.Business
{
    using FluentValidation;
    using Payroll.Contracts;
    using System;

    public class AddEmployeeRequestDtoValidator : AbstractValidator<AddEmployeeRequestDto>
    {
        public AddEmployeeRequestDtoValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.")
                .Matches("^[a-zA-Z ]*$").WithMessage("First name must be alphabetic and cannot contain special characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.")
                .Matches("^[a-zA-Z ]*$").WithMessage("First name must be alphabetic and cannot contain special characters.");

            RuleFor(r => r.UserName)
                .NotEmpty().WithMessage("Username is required")
                .Length(7, 30).WithMessage("Username must be between 7 and 30 characters")
                .Matches("^[a-zA-Z0-9]+$").WithMessage("Username must be alphanumeric characters only without spaces");

            RuleFor(r => r.Password)
                .NotEmpty().WithMessage("Password is Required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
                .MaximumLength(13).WithMessage("Password cannot exceed 13 characters")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character")
                .Must(x => !x.Contains(" ")).WithMessage("Password cannot contain spaces");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("Role must be either Admin, Manager, or Employee.");

            RuleFor(x => x.Department)
            .IsInEnum().WithMessage("Department must be either HR, IT, Sales, or Finance.");

            RuleFor(x => x.Salary)
            .GreaterThan(0).WithMessage("Salary must be greater than zero.")
            .LessThanOrEqualTo(1000000).WithMessage("Salary must be less than or equal to 1,000,000.");

            RuleFor(x => x.ManagerId)
                .Must(id => id == null || id != Guid.Empty).WithMessage("ManagerId must be a valid GUID or null.");
        }
    }
}
