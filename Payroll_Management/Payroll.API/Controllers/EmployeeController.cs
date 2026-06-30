namespace Payroll.API
{
    using Asp.Versioning;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Payroll.Business;
    using Payroll.Contracts;
    using Payroll.Domain;

    [Authorize(Roles = "Admin, Manager, Employee")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion(1.0)]
    [ApiVersion(2.0)]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        /// <summary>
        /// Gets the list of team members.
        /// Admins can view all employees, Managers can view employees in their department.
        /// </summary>
        /// <param name="query">Query parameters for filtering, sorting and pagination</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the list of team members</returns>
        [MapToApiVersion(2.0)]
        [HasPermission(PermissionType.VIEW_TEAM)]
        [HttpGet("team")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTeamMembers([FromQuery] QueryDto query,
                                                        CancellationToken cancellationToken = default)
        {
            List<EmployeeResponseDto> employees = await _employeeService.GetAllEmployeesAsync(query, cancellationToken);
            return Ok(employees);
        }

        /// <summary>
        /// Gets the personal information of the currently logged-in employee.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the personal information of the currently logged-in employee</returns>
        [MapToApiVersion(2.0)]
        [HasPermission(PermissionType.VIEW_PERSONAL_INFO)]
        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyPersonalInfo(CancellationToken cancellationToken)
        {
            EmployeeResponseDto employee = await _employeeService.GetEmployeeByIdAsync(null, cancellationToken);
            return Ok(employee);
        }

        /// <summary>
        /// Adds a new employee to the system. Only Admins can add new employees, and they can assign them to any department.
        /// </summary>
        /// <param name="employeeRequestDto">The employee information to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the created employee information</returns>
        [HasPermission(PermissionType.ADD_EMPLOYEE)]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddEmployee(AddEmployeeRequestDto employeeRequestDto, CancellationToken cancellationToken)
        {
            EmployeeResponseDto employee = await _employeeService.AddEmployeeAsync(employeeRequestDto, cancellationToken);
            return CreatedAtAction(nameof(GetMyPersonalInfo), new { id = employee.EmployeeId }, employee);
        }

        /// <summary>
        /// Updates the information of an existing employee.
        /// Admins can update any employee's information.
        /// Only salary, manager assignment and role can be updated through this endpoint.
        /// </summary>
        /// <param name="id">The ID of the employee to update</param>
        /// <param name="employeeRequestDto">The updated employee information</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the updated employee information</returns>
        [HasPermission(PermissionType.UPDATE_EMPLOYEE)]
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateEmployee(Guid id, UpdateEmployeeRequestDto employeeRequestDto, CancellationToken cancellationToken)
        {
            EmployeeResponseDto employee = await _employeeService.UpdateEmployeeAsync(id, employeeRequestDto, cancellationToken);
            return Ok(employee);
        }

        /// <summary>
        /// Deletes an existing employee.
        /// </summary>
        /// <param name="id">The ID of the employee to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns a success message</returns>
        [HasPermission(PermissionType.DELETE_EMPLOYEE)]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteEmployee(Guid id, CancellationToken cancellationToken)
        {
            _ = await _employeeService.DeleteEmployeeAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
