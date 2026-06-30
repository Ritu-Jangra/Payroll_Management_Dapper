namespace Payroll.Data
{
    using Dapper;
    using Payroll.Domain;
    using System.Data;
    using System.Data.Common;

    public class LeaveRepository : BaseRepository, ILeaveRepository
    {
        private readonly DbConnection _dbConnection;

        public LeaveRepository(DbConnection dbConnection) : base(dbConnection)
        {
            _dbConnection = dbConnection;
        }

        /// <summary>
        /// Gets all leaves, optionally filtered by employeeId or managerId.
        /// If employeeId is provided, it returns leaves for that employee.
        /// If managerId is provided, it returns leaves for employees under that manager. If both are null, it returns all leaves.
        /// </summary>
        /// <param name="employeeId">The ID of the employee to filter leaves by</param>
        /// <param name="managerId">The ID of the manager to filter leaves by</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns a list of leaves</returns>
        public async Task<List<Leave>> GetAllLeavesAsync(Guid? employeeId = null,
                                                         Guid? managerId = null,
                                                         DbTransaction? transaction = null,
                                                         CancellationToken cancellationToken = default)
        {
            IEnumerable<Leave> leavesQuery = await GetAllLeavesAsync(transaction);

            if (employeeId.HasValue)
            {
                leavesQuery = leavesQuery.Where(l => l.EmployeeId == employeeId.Value);
            }
            if (managerId.HasValue)
            {
                leavesQuery = leavesQuery.Where(l => l.Employee!.ManagerId == managerId.Value);
            }

            return leavesQuery.ToList();
        }

        /// <summary>
        /// Gets a leave by its ID, including the associated employee details. Returns null if not found.
        /// </summary>
        /// <param name="leaveId">The ID of the leave to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the leave if found, null otherwise</returns>
        public async Task<Leave?> GetLeaveByIdAsync(Guid leaveId, DbTransaction transaction, CancellationToken cancellationToken)
        {
            IEnumerable<Leave> allLeaves = await GetAllLeavesAsync(transaction);

            return allLeaves.FirstOrDefault(leave => leave.LeaveId == leaveId);
        }

        /// <summary>
        /// Adds a new leave to the database context.
        /// The actual save to the database will occur when SaveChangesAsync is called on the context.
        /// </summary>
        /// <param name="leave">Leave entity to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the added leave entity</returns>
        public async Task AddLeaveAsync(Leave leave, DbTransaction transaction, CancellationToken cancellationToken)
        {
            var sql = @"Insert into Leaves (LeaveId, Reason, StartDate, EndDate, Status, EmployeeId, ActionBy) Values
                                           (@LeaveId, @Reason, @StartDate, @EndDate, @Status, @EmployeeId, @ActionBy);";

            await _dbConnection.ExecuteAsync(sql, new
            {
                leave.LeaveId,
                Reason = leave.Reason.ToString(),
                leave.StartDate,
                leave.EndDate,
                Status = leave.Status.ToString(),
                leave.EmployeeId,
                leave.ActionBy
            }, transaction);
        }

        /// <summary>
        /// Updates an existing leave in the database context.
        /// </summary>
        /// <param name="leave">Leave entity to update</param>
        public async Task UpdateLeave(Leave leave, DbTransaction transaction)
        {
            var sql = @"UPDATE Leaves
                        SET
                            Reason = @Reason,
                            StartDate = @StartDate,
                            EndDate = @EndDate,
                            Status = @Status,
                            EmployeeId = @EmployeeId,
                            ActionBy = @ActionBy
                        WHERE LeaveId = @LeaveId;
                        ";
           await _dbConnection.ExecuteAsync(sql, new
           {
               leave.LeaveId,
               Reason = leave.Reason.ToString(),
               leave.StartDate,
               leave.EndDate,
               Status = leave.Status.ToString(),
               leave.EmployeeId,
               leave.ActionBy
           }, transaction);
        }

        /// <summary>
        /// Ensures that there are no overlapping leave requests for the same employee within the specified date range, excluding a specific leave ID if provided (useful for updates).
        /// </summary>
        /// <param name="employeeId">The ID of the employee to check for overlapping leaves</param>
        /// <param name="startDate">The start date of the leave request</param>
        /// <param name="endDate">The end date of the leave request</param>
        /// <param name="excludeLeaveId">The ID of the leave to exclude from the check (for updates)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns true if there is an overlapping leave, false otherwise</returns>
        public async Task<bool> ExistsOverlapAsync(Guid employeeId,
                                                   DateTime startDate,
                                                   DateTime endDate,
                                                   Guid? excludeLeaveId = null,
                                                   DbTransaction? transaction = null,
                                                   CancellationToken cancellationToken = default)
        {
            //Ensure that the new leave does not overlap with existing leaves for the same employee, excluding the current leave if updating
            return await _dbConnection.QuerySingleAsync<bool>("SELECT dbo.CheckLeavesOverlap(@EmployeeId, @StartDate, @EndDate, @ExcludedLeaveId)",
                                                                new
                                                                {
                                                                    EmployeeId = employeeId,
                                                                    StartDate = startDate,
                                                                    EndDate = endDate,
                                                                    ExcludedLeaveId = excludeLeaveId,
                                                                },
                                                                transaction);
        }

        /// <summary>
        /// Deletes a leave from the database context. 
        /// The actual deletion from the database will occur when SaveChangesAsync is called on the context.
        /// </summary>
        /// <param name="leave">Leave entity to delete</param>
        public async Task DeleteLeave(Leave leave, DbTransaction transaction)
        {
            string sql = "Delete from Leaves where LeaveId = @LeaveId";
            await _dbConnection.ExecuteAsync(sql, new { leave.LeaveId }, transaction);
        }

        public async Task<Guid?> GetManagerId(Guid employeeId, DbTransaction? transaction)
        {
            string sql = "Select e.ManagerId from Employees e where e.EmployeeId = @EmployeeId";

            return await _dbConnection.QueryFirstOrDefaultAsync<Guid?>(sql, new { EmployeeId = employeeId }, transaction);
        }

        #region Private Routine

        private async Task<IEnumerable<Leave>> GetAllLeavesAsync(DbTransaction? transaction)
        {
            const string sql = "dbo.usp_GetLeaveDetails";

            return await _dbConnection.QueryAsync<
                Leave,
                Employee,
                Employee,
                Employee,
                Leave>(
                sql,
                (leave, employee, manager, approver) =>
                {
                    if (employee != null && manager != null)
                        employee.Manager = manager;

                    leave.Employee = employee;

                    leave.Approver = approver?.EmployeeId != Guid.Empty
                        ? approver
                        : null;

                    return leave;
                },
                splitOn: "EmployeeId,EmployeeId,EmployeeId",
                commandType: CommandType.StoredProcedure,
                transaction: transaction
            );
        }

        #endregion Private Routine
    }
}