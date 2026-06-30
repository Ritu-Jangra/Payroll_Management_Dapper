namespace Payroll.Business
{
    using Payroll.Contracts;

    public interface IEmployeeService
    {
        Task<List<EmployeeResponseDto>> GetAllEmployeesAsync(QueryDto query, CancellationToken cancellationToken);

        Task<EmployeeResponseDto> GetEmployeeByIdAsync(Guid? employeeId = null, CancellationToken cancellationToken = default);

        Task<EmployeeResponseDto> AddEmployeeAsync(AddEmployeeRequestDto employeeRequestDto, CancellationToken cancellationToken);

        Task<EmployeeResponseDto> UpdateEmployeeAsync(Guid employeeId, UpdateEmployeeRequestDto employeeRequestDto, CancellationToken cancellationToken);

        Task<bool> DeleteEmployeeAsync(Guid employeeId, CancellationToken cancellationToken);
    }
}
