namespace Payroll.Data
{
    using Payroll.Domain;
    using System.Data.Common;

    public interface ILeaveRepository
    {
        Task<List<Leave>> GetAllLeavesAsync(Guid? employeeId = null,
                                            Guid? managerId = null,
                                            DbTransaction? transaction = null,
                                            CancellationToken cancellationToken = default);

        Task<Leave?> GetLeaveByIdAsync(Guid leaveId, DbTransaction transaction, CancellationToken cancellationToken);

        Task AddLeaveAsync(Leave leave, DbTransaction transaction, CancellationToken cancellationToken);

        Task UpdateLeave(Leave leave, DbTransaction transaction);

        Task DeleteLeave(Leave leave, DbTransaction transaction);

        Task<Guid?> GetManagerId(Guid employeeId, DbTransaction transaction);

        Task<bool> ExistsOverlapAsync(Guid employeeId,
                                      DateTime startDate,
                                      DateTime endDate,
                                      Guid? excludeLeaveId = null,
                                      DbTransaction? transaction = null,
                                      CancellationToken cancellationToken = default);
    }
}
