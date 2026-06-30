namespace Payroll.Data
{
    using Payroll.Domain;
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Threading.Tasks;

    public interface IEmployeeRepository
    {
        Task<List<Employee>> GetAllEmployeesAsync(Guid? adminId = null, Guid? managerId = null, Guid? employeeId = null, CancellationToken cancellationToken = default);

        Task<Employee?> GetEmployeeUsernameAsync(string username, CancellationToken cancellationToken);

        Task<Employee?> GetEmployeeWithPermissionsAsync(Guid userId, CancellationToken cancellationToken);

        Task<Employee?> GetEmployeeByIdAsync(Guid employeeId, DbTransaction transaction, CancellationToken cancellationToken);

        Task<Employee?> GetEmployeeDetailsAsync(Guid employeeId, CancellationToken cancellationToken);

        Task AddEmployeeAsync(Employee employee, DbTransaction transaction, CancellationToken cancellationToken);

        Task UpdateEmployee(Employee employee, DbTransaction transaction);

        Task DeleteEmployee(Employee employee, DbTransaction transaction);

        Task<bool> EmployeeUserNameExistsAsync(string userName, DbTransaction? transaction, CancellationToken cancellationToken);

        Task<bool> EmployeeEmailExistsAsync(string email, Guid? excludeEmployeeId = null, DbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<bool> EmployeeUserNameExistsInDepartmentAsync(string userName, DepartmentType department, Guid? excludeEmployeeId = null, DbTransaction? transaction = null, CancellationToken cancellationToken = default);

        Task<Guid> GetEmployeeIdByUserNameAsync(string userName, CancellationToken cancellationToken);

        Task<Guid> GetRoleIdByRoleTypeAsync(RoleType roleType, DbTransaction transaction, CancellationToken cancellationToken);
    }
}
