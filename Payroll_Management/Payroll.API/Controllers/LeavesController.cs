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
    public class LeavesController : ControllerBase
    {
        private readonly ILeaveService _leaveService;

        public LeavesController(ILeaveService leaveService)
        {
            _leaveService = leaveService;
        }

        /// <summary>
        /// Gets the leaves of team members. Only accessible to Managers and Admins.
        /// </summary>
        /// <param name="query">query parameters for filtering, sorting and pagination</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>returns the leaves of team members</returns>
        [MapToApiVersion(2.0)]
        [HasPermission(PermissionType.VIEW_TEAM_LEAVES)]
        [HttpGet("team")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTeamsLeaves([FromQuery] QueryDto query,
                                                        CancellationToken cancellationToken)
        {
            List<LeaveResponseDto> leaves = await _leaveService.GetTeamMembersLeavesAsync(query, cancellationToken);
            return Ok(leaves);
        }

        [HasPermission(PermissionType.VIEW_MY_LEAVES)]
        [HttpGet("my")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyLeaves([FromQuery] QueryDto query,
                                                      CancellationToken cancellationToken)
        {
            List<LeaveResponseDto> leaves = await _leaveService.GetMyLeavesAsync(query, cancellationToken);
            return Ok(leaves);
        }

        /// <summary>
        /// Applies for leave. 
        /// Only accessible to Employees, Managers.
        /// </summary>
        /// <param name="leaveRequestDto">leave request data</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>returns the result of the leave application</returns>
        [HasPermission(PermissionType.APPLY_LEAVE)]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApplyLeaves(ApplyLeaveRequestDto leaveRequestDto, CancellationToken cancellationToken)
        {
            LeaveResponseDto leave = await _leaveService.ApplyLeaveAsync(leaveRequestDto, cancellationToken);
            return CreatedAtAction(nameof(GetMyLeaves), new { id = leave.LeaveId }, leave);
        }

        /// <summary>
        /// Updates a leave request. Only accessible to Employees, Managers.
        /// </summary>
        /// <param name="id">leave request id</param>
        /// <param name="leaveRequestDto">leave request data</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>returns the result of the leave update</returns>
        [HasPermission(PermissionType.UPDATE_LEAVE)]
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateLeave(Guid id, UpdateLeaveRequestDto leaveRequestDto, CancellationToken cancellationToken)
        {
            LeaveResponseDto leave = await _leaveService.UpdateLeaveAsync(id, leaveRequestDto, cancellationToken);
            return Ok(leave);
        }

        /// <summary>
        /// Deletes a leave request. Only accessible to Employees, Managers.
        /// </summary>
        /// <param name="id">leave request id</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>returns the result of the leave deletion</returns>
        [HasPermission(PermissionType.DELETE_LEAVE)]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteLeave(Guid id, CancellationToken cancellationToken)
        {
            _ = await _leaveService.DeleteLeaveAsync(id, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Approves a leave request. Only accessible to Managers, Admins.
        /// </summary>
        /// <param name="id">leave request id</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>returns the result of the leave approval</returns>
        [MapToApiVersion(2.0)]
        [HasPermission(PermissionType.APPROVE_LEAVE)]
        [HttpPut("{id}/approve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApproveLeave(Guid id, CancellationToken cancellationToken)
        {
            LeaveResponseDto? leave = await _leaveService.ApproveLeaveAsync(id, cancellationToken);
            return Ok(leave);
        }

        /// <summary>
        /// Rejects a leave request. Only accessible to Managers, Admins.
        /// </summary>
        /// <param name="id">leave request id</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>returns the result of the leave rejection</returns>
        [MapToApiVersion(2.0)]
        [HasPermission(PermissionType.REJECT_LEAVE)]
        [HttpPut("{id}/reject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RejectLeave(Guid id, CancellationToken cancellationToken)
        {
            LeaveResponseDto? leave = await _leaveService.RejectLeaveAsync(id, cancellationToken);
            return Ok(leave);
        }
    }
}
