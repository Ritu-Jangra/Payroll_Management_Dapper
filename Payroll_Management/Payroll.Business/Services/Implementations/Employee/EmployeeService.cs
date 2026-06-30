namespace Payroll.Business
{
    using AutoMapper;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Payroll.Contracts;
    using Payroll.Data;
    using Payroll.Domain;
    using System.Data.Common;
    using System.Globalization;
    using System.Threading;
    using System.Transactions;

    public class EmployeeService : IEmployeeService
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;
        private readonly IPasswordService _passwordService;

        public EmployeeService(ICurrentUserService currentUserService,
                               IEmployeeRepository employeeRepository,
                               IMapper mapper,
                               IUnitOfWork unitOfWork,
                               ILogger<EmployeeService> logger,
                               IPasswordService passwordService)
        {
            _currentUserService = currentUserService;
            _employeeRepository = employeeRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _passwordService = passwordService;
        }

        /// <summary>
        /// Gets a list of employees based on the current user's role and the provided query parameters for filtering, sorting, and pagination.
        /// </summary>
        /// <param name="query">Query parameters for filtering, sorting, and pagination</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns a list of employee response DTOs</returns>
        public async Task<List<EmployeeResponseDto>> GetAllEmployeesAsync(QueryDto query, CancellationToken cancellationToken)
        {
            List<Employee> employees = _currentUserService.Role switch
            {
                RoleType.Admin => await _employeeRepository.GetAllEmployeesAsync(_currentUserService.UserId, null, null, cancellationToken), // Admin can see all employees
                RoleType.Manager => await _employeeRepository.GetAllEmployeesAsync(null, _currentUserService.UserId, null, cancellationToken), // Managers see only their team members
                _ => await _employeeRepository.GetAllEmployeesAsync(null, null, _currentUserService.UserId, cancellationToken) // Employees see only their own details
            };

            return GetFilteredEmployeeResponseDto(employees, query);
        }

        /// <summary>
        /// Gets the details of a specific employee by their ID.
        /// If no ID is provided, it returns the details of the currently logged-in employee.
        /// </summary>
        /// <param name="employeeId">The ID of the employee to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the employee response DTO</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the employee is not found</exception>
        public async Task<EmployeeResponseDto> GetEmployeeByIdAsync(Guid? employeeId = null, CancellationToken cancellationToken = default)
        {
            var employee = await _employeeRepository.GetEmployeeDetailsAsync(employeeId ?? _currentUserService.UserId, cancellationToken);

            if (employee == null)
            {
                _logger.LogWarning("Employee with ID: {EmployeeId} not found.", employeeId ?? _currentUserService.UserId);
                throw new KeyNotFoundException($"Employee with ID '{employeeId ?? _currentUserService.UserId}' not found.");
            }

            return _mapper.Map<EmployeeResponseDto>(employee);
        }

        /// <summary>
        /// Adds a new employee to the system after performing necessary validations.
        /// The method ensures that the username and email are unique and that the username does not already exist within the same department.
        /// It also sets the manager based on the role of the employee being added. The operation is performed within a transaction to ensure data integrity.
        /// </summary>
        /// <param name="employeeRequestDto">Employee request DTO</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the employee response DTO</returns>
        /// <exception cref="BadHttpRequestException">Thrown when employee validation fails</exception>
        public async Task<EmployeeResponseDto> AddEmployeeAsync(AddEmployeeRequestDto employeeRequestDto, CancellationToken cancellationToken)
        {
            Guid employeeId = Guid.Empty;
            _logger.LogInformation("Adding new employee with UserName: {UserName}", _currentUserService.UserName);

            await _unitOfWork.ExecuteInTransactionAsync(async (transaction, token) =>
            {
                if (await CheckEmployeeValidations(employeeRequestDto.UserName, employeeRequestDto.Email, employeeRequestDto.Department, false, null, transaction, token))
                {
                    _logger.LogInformation("Validation passed for UserName: {UserName}, Email: {Email}, Department: {Department}",
                                            employeeRequestDto.UserName, employeeRequestDto.Email, employeeRequestDto.Department);

                    var employee = _mapper.Map<Employee>(employeeRequestDto);
                    employee.EmployeeId = Guid.NewGuid();
                    employee.RoleId = await _employeeRepository.GetRoleIdByRoleTypeAsync(employeeRequestDto.Role, transaction, token);
                    employee.PasswordHash = _passwordService.HashPassword(employee);
                    employee.ManagerId = SetManagerByRole(employeeRequestDto.Role, employeeRequestDto.ManagerId);

                    await _employeeRepository.AddEmployeeAsync(employee, transaction, token);

                    employeeId = employee.EmployeeId;
                }
                else
                {
                    _logger.LogError("Validation failed for UserName: {UserName}, Email: {Email}, Department: {Department}",
                                      employeeRequestDto.UserName, employeeRequestDto.Email, employeeRequestDto.Department);
                    throw new BadHttpRequestException("Employee validation failed. Please check the logs for details.");
                }

            }, cancellationToken);

            _logger.LogInformation("Employee with UserName: {UserName} added successfully.", employeeRequestDto.UserName);
            return await GetEmployeeByIdAsync(employeeId, cancellationToken);
        }

        /// <summary>
        /// Updates the details of an existing employee based on the provided employee ID and update request DTO.
        /// </summary>
        /// <param name="employeeId">The ID of the employee to update</param>
        /// <param name="employeeRequestDto">The employee update request DTO</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the updated employee response DTO</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the employee is not found</exception>
        /// <exception cref="BadHttpRequestException">Thrown when employee validation fails</exception>
        public async Task<EmployeeResponseDto> UpdateEmployeeAsync(Guid employeeId, UpdateEmployeeRequestDto employeeRequestDto, CancellationToken cancellationToken)
        {
            Guid updatedEmployeeId = Guid.Empty;
            _logger.LogInformation("Updating employee with ID: {EmployeeId}", employeeId);

            await _unitOfWork.ExecuteInTransactionAsync(async (transaction, token) =>
            {
                var existingEmployee = await _employeeRepository.GetEmployeeByIdAsync(employeeId, transaction, token);

                if (existingEmployee == null)
                {                    
                    _logger.LogWarning("Employee with ID: {EmployeeId} not found for update.", employeeId);
                    throw new KeyNotFoundException($"Employee with ID '{employeeId}' not found.");
                }

                // Only proceed with update if the employee exists and validations pass
                if (await CheckEmployeeValidations(existingEmployee.UserName, existingEmployee.Email, existingEmployee.Department, true, employeeId, transaction, token))
                {
                    _mapper.Map(employeeRequestDto, existingEmployee);
                    existingEmployee.RoleId = await _employeeRepository.GetRoleIdByRoleTypeAsync(employeeRequestDto.Role, transaction, token);
                    existingEmployee.ManagerId = SetManagerByRole(employeeRequestDto.Role, employeeRequestDto.ManagerId);

                    await _employeeRepository.UpdateEmployee(existingEmployee, transaction);

                    updatedEmployeeId = existingEmployee.EmployeeId;
                }
                else
                {
                    _logger.LogError("Update failed for EmployeeId: {EmployeeId}. Employee not found or validation failed.", employeeId);
                    throw new BadHttpRequestException($"Employee with ID '{employeeId}' not found or validation failed.");
                }

            }, cancellationToken);

            _logger.LogInformation("Employee with ID: {EmployeeId} updated successfully.", employeeId);
            return await GetEmployeeByIdAsync(updatedEmployeeId, cancellationToken);
        }

        /// <summary>
        /// Deletes an employee from the system based on the provided employee ID.
        /// The method first checks if the employee exists and then proceeds to delete it within a transaction.
        /// </summary>
        /// <param name="employeeId">The ID of the employee to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns true if the employee was deleted successfully, otherwise false</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the employee is not found</exception>
        public async Task<bool> DeleteEmployeeAsync(Guid employeeId, CancellationToken cancellationToken)
        {
            bool isDeleted = false;
            _logger.LogInformation("Attempting to delete employee with ID: {EmployeeId}", employeeId);

            await _unitOfWork.ExecuteInTransactionAsync(async (transaction, token) =>
            {
                var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId, transaction, token);

                if (employee != null)
                {
                    await _employeeRepository.DeleteEmployee(employee, transaction);
                    isDeleted = true;
                }
                else
                {
                    _logger.LogError("Delete failed for EmployeeId: {EmployeeId}. Employee not found.", employeeId);
                    throw new KeyNotFoundException($"Employee with ID '{employeeId}' not found.");
                }
            }, cancellationToken);

            _logger.LogInformation("Employee with ID: {EmployeeId} deletion status: {IsDeleted}", employeeId, isDeleted);
            return isDeleted;
        }

        #region Private Helper Methods

        private List<EmployeeResponseDto> GetFilteredEmployeeResponseDto(IEnumerable<Employee> employees,
                                                                        QueryDto query)
        {
            if (employees == null || !employees.Any() || employees.Any(e => e == null))
            {
                _logger.LogWarning("No employees found.");
                throw new KeyNotFoundException("No employees found.");
            }

            //filtering the data
            if (!string.IsNullOrEmpty(query.FilterOn) && !string.IsNullOrEmpty(query.FilterQuery))
            {
                employees = query.FilterOn.ToLower() switch
                {
                    "username" => employees.Where(e => e.UserName.ToLower().Contains(query.FilterQuery.ToLower())),
                    "firstname" => employees.Where(e => e.FirstName.ToLower().Contains(query.FilterQuery.ToLower())),
                    "lastname" => employees.Where(e => e.LastName!.ToLower().Contains(query.FilterQuery.ToLower())),
                    "email" => employees.Where(e => e.Email.ToLower().Contains(query.FilterQuery.ToLower())),
                    "department" => employees.Where(e => e.Department.ToString().ToLower().Contains(query.FilterQuery.ToLower())),
                    "manager" => employees.Where(e => e.Manager != null && (e.Manager.FirstName + " " + e.Manager.LastName).ToLower().Contains(query.FilterQuery.ToLower())),
                    _ => employees
                };
            }

            //Sorting
            if (!string.IsNullOrEmpty(query.SortBy))
            {
                // Only allow sorting by known properties
                var validSortColumns = new[] { "FirstName", "LastName", "UserName", "Role" };
                if (validSortColumns.Contains(query.SortBy, StringComparer.OrdinalIgnoreCase))
                {
                    ApplySorting(ref employees!, query.SortBy, query.IsAscending);
                }
            }

            //Pagination
            employees = employees.Skip((query.PageNum - 1) * query.PageSize).Take(query.PageSize);

            // Mapping to DTO
            return _mapper.Map<List<EmployeeResponseDto>>(employees);
        }

        private static void ApplySorting(ref IEnumerable<Employee> employees,
                                        string sortBy,
                                        bool isAscending = false)
        {
            employees = sortBy.ToLower() switch
            {
                "FirstName" => isAscending ? employees.OrderBy(x => x.FirstName) : employees.OrderByDescending(x => x.FirstName),
                "LastName" => isAscending ? employees.OrderBy(x => x.LastName) : employees.OrderByDescending(x => x.LastName),
                "UserName" => isAscending ? employees.OrderBy(x => x.UserName) : employees.OrderByDescending(x => x.UserName),
                "Role" => isAscending ? employees.OrderBy(x => x.Role?.Name) : employees.OrderByDescending(x => x.Role?.Name),
                _ => employees
            };
        }

        private async Task<bool> CheckEmployeeValidations(string userName,
                                                          string email,
                                                          DepartmentType department,
                                                          bool forUpdate = false,
                                                          Guid? excludeEmployeeId = null,
                                                          DbTransaction? transaction = null,
                                                          CancellationToken cancellationToken = default)
        {
            bool isValid = true;

            if (!forUpdate
                && !string.IsNullOrEmpty(userName)
                && await _employeeRepository.EmployeeUserNameExistsAsync(userName, transaction, cancellationToken))
            {
                _logger.LogError("Validation failed: UserName {UserName} already exists.", userName);
                throw new InvalidOperationException($"UserName '{userName}' already exists.");
            }

            if (!string.IsNullOrEmpty(email) && await _employeeRepository.EmployeeEmailExistsAsync(email, excludeEmployeeId, transaction, cancellationToken))
            {
                _logger.LogError("Validation failed: Email {Email} already exists.", email);
                throw new InvalidOperationException($"Email '{email}' already exists.");
            }

            if (!string.IsNullOrEmpty(email) && await _employeeRepository.EmployeeUserNameExistsInDepartmentAsync(userName, department, excludeEmployeeId, transaction, cancellationToken))
            {
                _logger.LogError("Validation failed: UserName {UserName} already exists in Department {Department}.", userName, department);
                throw new InvalidOperationException($"UserName '{userName}' already exists in Department '{department}'.");
            }

            return isValid;
        }

        private Guid? SetManagerByRole(RoleType roleType, Guid? managerId)
        {
            return roleType switch
            {
                RoleType.Employee => managerId, // Employees must have a manager
                RoleType.Manager => managerId == null ? _currentUserService.UserId : managerId,  // Managers can have a manager (e.g., Admin)
                _ => null // Admins do not have a manager
            };
        }

        #endregion Private Helper Methods
    }
}