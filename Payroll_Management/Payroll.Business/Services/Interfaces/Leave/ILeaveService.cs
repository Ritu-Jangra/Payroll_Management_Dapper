namespace Payroll.Business
{
    using Payroll.Contracts;

    public interface ILeaveService
    {
        Task<List<LeaveResponseDto>> GetTeamMembersLeavesAsync(QueryDto query, CancellationToken cancellationToken);

        Task<List<LeaveResponseDto>> GetMyLeavesAsync(QueryDto query, CancellationToken cancellationToken);

        Task<LeaveResponseDto> GetLeaveByIdAsync(Guid leaveId, CancellationToken cancellationToken);

        Task<LeaveResponseDto> ApplyLeaveAsync(ApplyLeaveRequestDto leaveRequestDto, CancellationToken cancellationToken);

        Task<LeaveResponseDto> UpdateLeaveAsync(Guid leaveId, UpdateLeaveRequestDto leaveRequestDto, CancellationToken cancellationToken);

        Task<LeaveResponseDto> ApproveLeaveAsync(Guid leaveId, CancellationToken cancellationToken);

        Task<LeaveResponseDto> RejectLeaveAsync(Guid leaveId, CancellationToken cancellationToken);

        Task<bool> DeleteLeaveAsync(Guid leaveId, CancellationToken cancellationToken);
    }
}
