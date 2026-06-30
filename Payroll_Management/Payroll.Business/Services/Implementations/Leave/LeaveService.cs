namespace Payroll.Business
{
    using AutoMapper;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Payroll.Business.Services;
    using Payroll.Contracts;
    using Payroll.Data;
    using Payroll.Domain;

    public class LeaveService : ILeaveService
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly ILeaveRepository _leaveRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;
        private readonly MemoryCacheEntryOptions options = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            SlidingExpiration = TimeSpan.FromMinutes(5)
        };
        private readonly IHubContext<LeaveNotificationClient, ILeaveNotificationClient> _hubContext;

        public LeaveService(ICurrentUserService currentUserService,
                            IMapper mapper,
                            ILeaveRepository leaveRepository,
                            IUnitOfWork unitOfWork,
                            ILogger<LeaveService> logger,
                            IMemoryCache cache,
                            IHubContext<LeaveNotificationClient, ILeaveNotificationClient> hubContext)
        {
            _currentUserService = currentUserService;
            _mapper = mapper;
            _leaveRepository = leaveRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cache = cache;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Gets leave requests of team members for managers and all leave requests for admins.
        /// </summary>
        /// <param name="query">Query parameters for filtering, sorting and pagination</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns a list of leave requests</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authorized to view the leave requests</exception>
        public async Task<List<LeaveResponseDto>> GetTeamMembersLeavesAsync(QueryDto query, CancellationToken cancellationToken)
        {
            string cacheKey = $"leaves_team_{_currentUserService.Role}_{_currentUserService.UserId}";

            List<Leave>? result = null;

            await _unitOfWork.ExecuteInTransactionAsync(async (transaction, token) =>
            {
                if (!_cache.TryGetValue(cacheKey, out List<Leave>? leaves))
                {
                    leaves = _currentUserService.Role switch
                    {
                        RoleType.Admin => await _leaveRepository.GetAllLeavesAsync(null, null, transaction, cancellationToken), // Admin can see all employees leaves
                        RoleType.Manager => await _leaveRepository.GetAllLeavesAsync(null, _currentUserService.UserId, transaction, cancellationToken), // Managers see only their team members' leaves
                        _ => throw new UnauthorizedAccessException("You are not authorized to view team members' leave requests.") // Employees cannot see other employees' leaves
                    };

                    _cache.Set(cacheKey, leaves, options);
                }
                result = leaves;
            }, cancellationToken);

            return GetFilteredLeaveResponseDto(result, query);
        }

        /// <summary>
        /// Gets leave requests of the current user (employee or manager).
        /// Employees see only their own leaves, while managers see their own leaves.
        /// </summary>
        /// <param name="query">Query parameters for filtering, sorting and pagination</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns a list of leave requests</returns>
        public async Task<List<LeaveResponseDto>> GetMyLeavesAsync(QueryDto query, CancellationToken cancellationToken)
        {
            string cacheKey = $"leaves_my_{_currentUserService.Role}_{_currentUserService.UserId}";

            List<Leave>? result = null;

            await _unitOfWork.ExecuteInTransactionAsync(async (transaction, token) =>
            {
                if (!_cache.TryGetValue(cacheKey, out List<Leave>? leaves))
                {
                    leaves = await _leaveRepository.GetAllLeavesAsync(_currentUserService.UserId, null, transaction, cancellationToken);
                    _cache.Set(cacheKey, leaves, options);
                }

                result = leaves;
            }, cancellationToken);

            return GetFilteredLeaveResponseDto(result, query);
        }

        /// <summary>
        /// Gets a specific leave request by its ID.
        /// </summary>
        /// <param name="leaveId">The ID of the leave request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the leave request details</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the leave request is not found</exception>
        public async Task<LeaveResponseDto> GetLeaveByIdAsync(Guid leaveId, CancellationToken cancellationToken)
        {
            Leave? leave = null;

            await _unitOfWork.ExecuteInTransactionAsync(async (transaction, token) =>
            {
                leave = await _leaveRepository.GetLeaveByIdAsync(leaveId, transaction, cancellationToken);

            }, cancellationToken);

            if (leave == null)
            {
                _logger.LogError("Leave request not found: LeaveId={LeaveId}, UserId={UserId}", leaveId, _currentUserService.UserId);
                throw new KeyNotFoundException(LeaveExceptionMessage[nameof(KeyNotFoundException)]);
            }

            return _mapper.Map<LeaveResponseDto>(leave);
        }

        /// <summary>
        /// Applies for a new leave request.
        /// Employees can apply for leave, while managers can apply for their own leave.
        /// </summary>
        /// <param name="leaveRequestDto">The leave request information</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the created leave request details</returns>
        /// <exception cref="BadHttpRequestException">Thrown when the leave request overlaps with an existing leave</exception>
        public async Task<LeaveResponseDto> ApplyLeaveAsync(ApplyLeaveRequestDto leaveRequestDto, CancellationToken cancellationToken)
        {
            Guid leaveId = Guid.Empty;
            _logger.LogInformation("Applying for leave: EmployeeId={EmployeeId}, StartDate={StartDate}, EndDate={EndDate}",
                                   _currentUserService.UserId, leaveRequestDto.StartDate, leaveRequestDto.EndDate);

            await _unitOfWork.ExecuteInTransactionAsync(async (transaction, token) =>
            {
                var overlap = await _leaveRepository.ExistsOverlapAsync(_currentUserService.UserId,
                                                                        leaveRequestDto.StartDate,
                                                                        leaveRequestDto.EndDate,
                                                                        null,
                                                                        transaction,
                                                                        token);

                // only apply if there are no overlapping leaves for the same employee
                if (!overlap)
                {
                    var leave = _mapper.Map<Leave>(leaveRequestDto);
                    leave.LeaveId = Guid.NewGuid();
                    leave.EmployeeId = _currentUserService.UserId;
                    leave.ActionBy = await _leaveRepository.GetManagerId(_currentUserService.UserId, transaction);

                    await _leaveRepository.AddLeaveAsync(leave, transaction, token);

                    InvalidateLeaveCache();
                    leaveId = leave.LeaveId;
                    if (leave.ActionBy != Guid.Empty)
                    {
                        await _hubContext.Clients
                            .User(leave.ActionBy.ToString()!)
                            .ReceiveLeaveNotification(leave);
                    }
                }
                else
                {
                    _logger.LogError("Overlapping leave request exists for EmployeeId={EmployeeId} between StartDate={StartDate} and EndDate={EndDate}",
                                       _currentUserService.UserId, leaveRequestDto.StartDate, leaveRequestDto.EndDate);
                    throw new BadHttpRequestException(LeaveExceptionMessage[nameof(BadHttpRequestException)]);
                }

            }, cancellationToken);

            _logger.LogInformation("Leave request applied successfully: LeaveId={LeaveId}, EmployeeId={EmployeeId}", leaveId, _currentUserService.UserId);
            return await GetLeaveByIdAsync(leaveId, cancellationToken);
        }

        /// <summary>
        /// Updates an existing leave request.
        /// </summary>
        /// <param name="leaveId">The ID of the leave request to update</param>
        /// <param name="leaveRequestDto">The updated leave request information</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the updated leave request details</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the leave request is not found</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authorized to update the leave request</exception>
        /// <exception cref="BadHttpRequestException">Thrown when the leave request overlaps with an existing leave</exception>
        public async Task<LeaveResponseDto> UpdateLeaveAsync(Guid leaveId,
                                                             UpdateLeaveRequestDto leaveRequestDto,
                                                             CancellationToken cancellationToken)
        {
            Guid updatedLeaveId = Guid.Empty;
            _logger.LogInformation("Updating leave request: LeaveId={LeaveId}, EmployeeId={EmployeeId}, StartDate={StartDate}, EndDate={EndDate}",
                                   leaveId, _currentUserService.UserId, leaveRequestDto.StartDate, leaveRequestDto.EndDate);

            await _unitOfWork.ExecuteInTransactionAsync(async (transaction, token) =>
            {
                var overlap = await _leaveRepository.ExistsOverlapAsync(_currentUserService.UserId,
                                                                        leaveRequestDto.StartDate,
                                                                        leaveRequestDto.EndDate,
                                                                        leaveId,
                                                                        transaction,
                                                                        token);

                // Only update if there are no overlapping leaves for the same employee, excluding the current leave
                if (!overlap)
                {
                    var leave = await _leaveRepository.GetLeaveByIdAsync(leaveId, transaction, token);

                    if (leave == null)
                    {
                        _logger.LogError("Leave request not found: LeaveId={LeaveId}, EmployeeId={EmployeeId}", leaveId, _currentUserService.UserId);
                        throw new KeyNotFoundException(LeaveExceptionMessage[nameof(KeyNotFoundException)]);
                    }

                    if (IsLeaveActionAllowed(leave, true))
                    {
                        _mapper.Map(leaveRequestDto, leave);

                        await _leaveRepository.UpdateLeave(leave, transaction);

                        InvalidateLeaveCache();
                        updatedLeaveId = leave.LeaveId;
                    }
                    else
                    {
                        _logger.LogError("Unauthorized attempt to update leave request: LeaveId={LeaveId}, EmployeeId={EmployeeId}",
                                        leaveId, _currentUserService.UserId);
                        throw new UnauthorizedAccessException(LeaveExceptionMessage[nameof(UnauthorizedAccessException)]);
                    }
                }
                else
                {
                    _logger.LogError("Overlapping leave request exists for EmployeeId={EmployeeId} between StartDate={StartDate} and EndDate={EndDate} while updating LeaveId={LeaveId}",
                                    _currentUserService.UserId, leaveRequestDto.StartDate, leaveRequestDto.EndDate, leaveId);
                    throw new BadHttpRequestException(LeaveExceptionMessage[nameof(BadHttpRequestException)]);
                }

            }, cancellationToken);

            _logger.LogInformation("Leave request updated successfully: LeaveId={LeaveId}, EmployeeId={EmployeeId}", updatedLeaveId, _currentUserService.UserId);
            return await GetLeaveByIdAsync(updatedLeaveId, cancellationToken);
        }

        /// <summary>
        /// Approves a pending leave request.
        /// </summary>
        /// <param name="leaveId">The ID of the leave request to approve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the approved leave request details</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the leave request is not found</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authorized to approve the leave request</exception>
        public async Task<LeaveResponseDto> ApproveLeaveAsync(Guid leaveId, CancellationToken cancellationToken)
        {
            Guid approvedLeaveId = Guid.Empty;
            _logger.LogInformation("Approving leave request: LeaveId={LeaveId}, ApproverId={ApproverId}", leaveId, _currentUserService.UserId);

            await _unitOfWork.ExecuteInTransactionAsync(async (transaction, token) =>
            {
                var leave = await _leaveRepository.GetLeaveByIdAsync(leaveId, transaction, token);

                if (leave == null)
                {
                    _logger.LogError("Leave request not found: LeaveId={LeaveId}, ApproverId={ApproverId}", leaveId, _currentUserService.UserId);
                    throw new KeyNotFoundException(LeaveExceptionMessage[nameof(KeyNotFoundException)]);
                }

                if (IsLeaveActionAllowed(leave, false))
                {
                    leave.Status = StatusType.Approved;
                    leave.ActionBy = _currentUserService.UserId;

                    await _leaveRepository.UpdateLeave(leave, transaction);

                    InvalidateLeaveCache();
                    approvedLeaveId = leave.LeaveId;
                }
                else
                {
                    _logger.LogError("Unauthorized attempt to approve leave request: LeaveId={LeaveId}, ApproverId={ApproverId}", leaveId, _currentUserService.UserId);
                    throw new UnauthorizedAccessException(LeaveExceptionMessage[nameof(UnauthorizedAccessException)]);
                }

            }, cancellationToken);

            _logger.LogInformation("Leave request approved successfully: LeaveId={LeaveId}, ApproverId={ApproverId}", approvedLeaveId, _currentUserService.UserId);
            return await GetLeaveByIdAsync(approvedLeaveId, cancellationToken);
        }

        /// <summary>
        /// Rejects a pending leave request.
        /// </summary>
        /// <param name="leaveId">The ID of the leave request to reject</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the rejected leave request details</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the leave request is not found</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authorized to reject the leave request</exception>
        public async Task<LeaveResponseDto> RejectLeaveAsync(Guid leaveId, CancellationToken cancellationToken)
        {
            Guid rejectedLeaveId = Guid.Empty;
            _logger.LogInformation("Rejecting leave request: LeaveId={LeaveId}, ApproverId={ApproverId}", leaveId, _currentUserService.UserId);

            await _unitOfWork.ExecuteInTransactionAsync(async (transaction, token) =>
            {
                var leave = await _leaveRepository.GetLeaveByIdAsync(leaveId, transaction, token);

                if (leave == null)
                {
                    _logger.LogError("Leave request not found: LeaveId={LeaveId}, ApproverId={ApproverId}", leaveId, _currentUserService.UserId);
                    throw new KeyNotFoundException(LeaveExceptionMessage[nameof(KeyNotFoundException)]);
                }

                if (IsLeaveActionAllowed(leave))
                {
                    leave.Status = StatusType.Rejected;
                    leave.ActionBy = _currentUserService.UserId;

                    await _leaveRepository.UpdateLeave(leave, transaction);

                    InvalidateLeaveCache();
                    rejectedLeaveId = leave.LeaveId;
                }
                else
                {
                    _logger.LogError("Unauthorized attempt to reject leave request: LeaveId={LeaveId}, ApproverId={ApproverId}", leaveId, _currentUserService.UserId);
                    throw new UnauthorizedAccessException(LeaveExceptionMessage[nameof(UnauthorizedAccessException)]);
                }
            }, cancellationToken);

            _logger.LogInformation("Leave request rejected successfully: LeaveId={LeaveId}, ApproverId={ApproverId}", rejectedLeaveId, _currentUserService.UserId);
            return await GetLeaveByIdAsync(rejectedLeaveId, cancellationToken);
        }

        /// <summary>
        /// Deletes a pending leave request.
        /// </summary>
        /// <param name="leaveId">The ID of the leave request to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns true if the leave request was deleted successfully</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the leave request is not found</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authorized to delete the leave request</exception>
        public async Task<bool> DeleteLeaveAsync(Guid leaveId, CancellationToken cancellationToken)
        {
            bool isDeleted = false;
            _logger.LogInformation("Deleting leave request: LeaveId={LeaveId}, EmployeeId={EmployeeId}", leaveId, _currentUserService.UserId);

            await _unitOfWork.ExecuteInTransactionAsync(async (transaction, token) =>
            {
                var leave = await _leaveRepository.GetLeaveByIdAsync(leaveId, transaction, token);

                if (leave == null)
                {
                    _logger.LogError("Leave request not found for deletion: LeaveId={LeaveId}, EmployeeId={EmployeeId}", leaveId, _currentUserService.UserId);
                    throw new KeyNotFoundException(LeaveExceptionMessage[nameof(KeyNotFoundException)]);
                }

                if (IsLeaveActionAllowed(leave, true))
                {
                    await _leaveRepository.DeleteLeave(leave, transaction);
                    InvalidateLeaveCache();
                    isDeleted = true;
                }
                else
                {
                    _logger.LogError("Unauthorized attempt to delete leave request: LeaveId={LeaveId}, EmployeeId={EmployeeId}", leaveId, _currentUserService.UserId);
                    throw new UnauthorizedAccessException(LeaveExceptionMessage[nameof(UnauthorizedAccessException)]);
                }
            }, cancellationToken);

            _logger.LogInformation("Leave request deleted successfully: LeaveId={LeaveId}, EmployeeId={EmployeeId}", leaveId, _currentUserService.UserId);
            return isDeleted;
        }

        #region Private Helper Methods

        private List<LeaveResponseDto> GetFilteredLeaveResponseDto(IEnumerable<Leave>? leaves,
                                                                   QueryDto query)
        {
            if (leaves == null || !leaves.Any() || leaves.Any(l => l == null))
            {
                _logger.LogWarning("No leave requests found for the current user.");
                throw new KeyNotFoundException(LeaveExceptionMessage[nameof(KeyNotFoundException)]);
            }

            if (query != null)
            {
                //filtering the data
                if (!string.IsNullOrEmpty(query.FilterOn) && !string.IsNullOrEmpty(query.FilterQuery))
                {
                    ApplyFilters(ref leaves, query.FilterOn, query.FilterQuery);
                }

                //Sorting
                if (!string.IsNullOrEmpty(query.SortBy))
                {
                    // Only allow sorting by known properties
                    var validSortColumns = new[] { "Employee", "StartDate", "EndDate", "Status" };
                    if (validSortColumns.Contains(query.SortBy, StringComparer.OrdinalIgnoreCase))
                    {
                        ApplySorting(ref leaves!, query.SortBy, query.IsAscending);
                    }
                }
                else
                {
                    //fetch latest records first
                    leaves = leaves!.OrderByDescending(l => l.StartDate);
                }


                //Pagination
                query.PageNum = Math.Max(query.PageNum, 1);
                query.PageSize = Math.Max(query.PageSize, 1);
                leaves = leaves!.Skip((query.PageNum - 1) * query.PageSize).Take(query.PageSize);
            }

            // Mapping to DTO
            return _mapper.Map<List<LeaveResponseDto>>(leaves);
        }

        private static void ApplyFilters(ref IEnumerable<Leave> leaves,
                                         string filterOn,
                                         string filterQuery)
        {
            leaves = filterOn.ToLower() switch
            {
                "startdate" => leaves.Where(l => l.StartDate.ToString().Contains(filterQuery)),
                "enddate" => leaves.Where(l => l.EndDate.ToString().Contains(filterQuery)),
                "status" => leaves.Where(l => l.Status.ToString().ToLower().Contains(filterQuery.ToLower())),
                "employee" => leaves.Where(l => l.Employee!.FirstName.ToLower().Contains(filterQuery.ToLower()) || l.Employee.LastName!.ToLower().Contains(filterQuery.ToLower())),
                "approver" => leaves.Where(l => l.Approver!.FirstName.ToLower().Contains(filterQuery.ToLower()) || l.Approver.LastName!.ToLower().Contains(filterQuery.ToLower())),
                _ => leaves
            };
        }

        private static void ApplySorting(ref IEnumerable<Leave> leaves,
                                         string sortBy,
                                         bool isAscending = false)
        {
            leaves = sortBy.ToLower() switch
            {
                "employee" => isAscending ? leaves.OrderBy(x => x.Employee!.UserName) : leaves.OrderByDescending(x => x.Employee!.UserName),
                "startdate" => isAscending ? leaves.OrderBy(x => x.StartDate) : leaves.OrderByDescending(x => x.StartDate),
                "enddate" => isAscending ? leaves.OrderBy(x => x.EndDate) : leaves.OrderByDescending(x => x.EndDate),
                "status" => isAscending ? leaves.OrderBy(x => x.Status) : leaves.OrderByDescending(x => x.Status),
                _ => leaves
            };
        }

        private bool IsLeaveActionAllowed(Leave leave,
                                          bool forSelf = false)
        {
            if (leave.Status is StatusType.Approved or StatusType.Rejected)
                return false;

            // MANAGER ACTION (approve/reject)
            if (!forSelf)
            {
                return leave.EmployeeId != _currentUserService.UserId
                    && leave.Employee!.ManagerId == _currentUserService.UserId
                    && leave.Status == StatusType.Pending;
            }

            // SELF ACTION (create/update/delete)
            return leave.EmployeeId == _currentUserService.UserId
                && leave.Status == StatusType.Pending;
        }

        private static readonly Dictionary<string,string> LeaveExceptionMessage = new()
        {
            { nameof(KeyNotFoundException), "Leave request not found." },
            { nameof(UnauthorizedAccessException), "You are not authorized to perform this action on the leave request." },
            { nameof(BadHttpRequestException), "The leave request overlaps with an existing leave request for the specified dates." },
        };

        private void InvalidateLeaveCache()
        {
            _cache.Remove($"leaves_team");
            _cache.Remove($"leaves_my_{_currentUserService.UserId}");
        }

        #endregion Private Helper Methods
    }
}
