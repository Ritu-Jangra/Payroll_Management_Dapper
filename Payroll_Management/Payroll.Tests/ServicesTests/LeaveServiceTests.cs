namespace Payroll.Tests
{
    using AutoMapper;
    using FluentAssertions;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using Payroll.Business;
    using Payroll.Business.Services;
    using Payroll.Contracts;
    using Payroll.Data;
    using Payroll.Domain;
    using System;
    using System.Data.Common;
    using static LeavesTestData;

    [TestClass]
    public class LeaveServiceTests : BaseTests
    {
        #region Test Data

        private static ILeaveRepository GetMoqLeaveRepository(int instance, Guid userId)
        {
            var result = new Mock<ILeaveRepository>();

            List<Leave?>? leaves = GetListOfLeaves(instance);

            leaves = instance switch
            {
                0 or 6 or 7 => leaves?.Where(x => x!.EmployeeId == userId).ToList(),
                1 => leaves?.Where(x => x!.ActionBy == userId).ToList(),
                _ => leaves
            };

            Guid? employeeId = instance switch
            {
                0 or 6 or 7 => userId,
                _ => null
            };
            Guid? managerId = instance == 1 ? userId : null;

            result.Setup(x => x.GetAllLeavesAsync(employeeId ?? It.IsAny<Guid?>(), managerId ?? It.IsAny<Guid?>(), It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>())).ReturnsAsync(leaves!);
            result.Setup(x => x.GetLeaveByIdAsync(It.IsAny<Guid>(), It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>())).ReturnsAsync((Guid leaveId, DbTransaction transaction, CancellationToken token) => leaves?.FirstOrDefault(x => x!.LeaveId == leaveId));
            result.Setup(x => x.ExistsOverlapAsync(It.IsAny<Guid>(),
                                                   It.IsAny<DateTime>(),
                                                   It.IsAny<DateTime>(),
                                                   It.IsAny<Guid?>(),
                                                   It.IsAny<DbTransaction?>(),
                                                   It.IsAny<CancellationToken>())).ReturnsAsync(false);
            result.Setup(x => x.UpdateLeave(It.IsAny<Leave>(), It.IsAny<DbTransaction>()));
            result.Setup(x => x.DeleteLeave(It.IsAny<Leave>(), It.IsAny<DbTransaction>()))
                    .Callback<Leave, DbTransaction>((leave, transaction) =>
                    {
                        var leaveToRemove = leaves?
                            .FirstOrDefault(x => x!.LeaveId == leave.LeaveId);

                        if (leaveToRemove != null)
                        {
                            leaves!.Remove(leaveToRemove);
                        }
                    });

            switch (instance)
            {
                case 3 or 4 or 5:
                    result.Setup(x => x.AddLeaveAsync(It.IsAny<Leave>(), It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>())).ThrowsAsync(new KeyNotFoundException("Leaves data not found"));
                    break;
                default:
                    result.Setup(x => x.AddLeaveAsync(It.IsAny<Leave>(), It.IsAny<DbTransaction>(),It.IsAny<CancellationToken>()))
                            .Callback<Leave, DbTransaction, CancellationToken>((leave, _, __) =>
                            {
                                leave.LeaveId = Guid.NewGuid();
                                leaves!.Add(leave);
                            })
                            .Returns(Task.CompletedTask);
                    break;
            }

            return result.Object;
        }

        private static ICurrentUserService GetMoqCurrentUserService(int instance)
        {
            var result = new Mock<ICurrentUserService>();

            RoleType role = instance switch
            {
                0 => RoleType.Employee,
                1 or 3 or 5 or 6 => RoleType.Manager,
                2 or 4 or 7 => RoleType.Admin,
                _ => RoleType.Employee
            };

            result.Setup(x => x.UserId).Returns(new Guid($"52{instance % 2}C2653-44DA-{instance % 3}0CB-8590-B924B{instance % 4}765878"));
            result.Setup(x => x.UserName).Returns($"User{instance}");
            result.Setup(x => x.Role).Returns(role);

            return result.Object;
        }

        private static IMapper GetMoqMapper()
        {
            var result = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfiles>();
            }, NullLoggerFactory.Instance);

            return result.CreateMapper();
        }

        private static IUnitOfWork GetMoqUnitOfWork()
        {
            var result = new Mock<IUnitOfWork>();

            result.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<DbTransaction, CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
                     .Returns(async (Func<DbTransaction, CancellationToken, Task> func, CancellationToken token) =>
                     {
                         await func(null!, token);
                     });

            return result.Object;
        }

        private static ILogger<LeaveService> GetMoqLogger()
        {
            var result = new Mock<ILogger<LeaveService>>();

            result.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()));

            return result.Object;
        }

        private static IMemoryCache GetMoqMemoryCache()
        {
            var cacheMock = new Mock<IMemoryCache>();

            cacheMock
                .Setup(m => m.CreateEntry(It.IsAny<object>()))
                .Returns((object key) =>
                {
                    var entryMock = new Mock<ICacheEntry>();
                    entryMock.SetupAllProperties();
                    return entryMock.Object;
                });

            cacheMock
                .Setup(m => m.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny))
                .Returns(false);

            return cacheMock.Object;
        }

        private static IHubContext<LeaveNotificationClient, ILeaveNotificationClient> GetMockHubContext()
        {
            var clientMock = new Mock<ILeaveNotificationClient>();

            clientMock
                .Setup(x => x.ReceiveLeaveNotification(It.IsAny<Leave>()))
                .Returns(Task.CompletedTask);

            var clientsMock = new Mock<IHubClients<ILeaveNotificationClient>>();

            clientsMock
                .Setup(x => x.User(It.IsAny<string>()))
                .Returns(clientMock.Object);

            var hubContextMock = new Mock<IHubContext<LeaveNotificationClient, ILeaveNotificationClient>>();

            hubContextMock
                .Setup(x => x.Clients)
                .Returns(clientsMock.Object);

            return hubContextMock.Object;
        }

        private static ILeaveService GetMockLeaveService(int instance)
        {
            var currentUserService = GetMoqCurrentUserService(instance);
            var leaveRepository = GetMoqLeaveRepository(instance, currentUserService.UserId);
            var mapper = GetMoqMapper();
            var unitOfWork = GetMoqUnitOfWork();
            var logger = GetMoqLogger();
            var cache = GetMoqMemoryCache();
            var hubContext = GetMockHubContext();

            return new LeaveService(currentUserService, mapper, leaveRepository, unitOfWork, logger, cache, hubContext);
        }

        #endregion Test Data

        #region Test Methods

        #region GetTeamMembersLeavesAsync Tests

        [TestMethod]
        [DataRow(1, DisplayName = $"{nameof(GetAllLeavesAsync_PositiveTests)} - Instance 1 - User with RoleType Manager")]
        [DataRow(2, DisplayName = $"{nameof(GetAllLeavesAsync_PositiveTests)} - Instance 2 - User with RoleType Admin")]
        public async Task GetAllLeavesAsync_PositiveTests(int instance)
        {
            TestContext.Write("Arrange");
            var leaveService = GetMockLeaveService(instance);

            TestContext.Write("Act");
            var result = await leaveService.GetTeamMembersLeavesAsync(new QueryDto(), CancellationToken.None);

            TestContext.Write("Assert");
            Assert.IsNotNull(result);
            int expectedCount = instance == 1 ? 3 : 8; // Manager should see 3 leaves, Admin should see all 8
            Assert.AreEqual(expectedCount, result.Count);
        }

        [TestMethod]
        [DataRow(0, DisplayName = $"{nameof(GetAllLeavesAsync_NegativeTests)} - Instance 0 - User with RoleType Employee")]
        public async Task GetAllLeavesAsync_NegativeTests(int instance)
        {
            TestContext.Write("Arrange");
            var leaveService = GetMockLeaveService(instance);

            TestContext.Write("Act and Assert");
            _ = await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(async () => await leaveService.GetTeamMembersLeavesAsync(new QueryDto(), CancellationToken.None));
        }

        [TestMethod]
        [DataRow(3, DisplayName = $"{nameof(GetAllLeavesAsync_NoLeavesData_Tests)} - Instance 3 - User with RoleType Manager when leaves list is empty")]
        [DataRow(4, DisplayName = $"{nameof(GetAllLeavesAsync_NoLeavesData_Tests)} - Instance 4 - User with RoleType Admin when leaves is null")]
        [DataRow(5, DisplayName = $"{nameof(GetAllLeavesAsync_NoLeavesData_Tests)} - Instance 5 - User with RoleType Manager when leaves list contains null")]
        public async Task GetAllLeavesAsync_NoLeavesData_Tests(int instance)
        {
            TestContext.Write("Arrange");
            var leaveService = GetMockLeaveService(instance);

            TestContext.Write("Act and Assert");
            _ = await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () => await leaveService.GetTeamMembersLeavesAsync(new QueryDto(), CancellationToken.None));
        }

        #endregion GetTeamMembersLeavesAsync Tests

        #region GetMyLeavesAsync Tests

        [TestMethod]
        [DataRow(0, DisplayName = $"{nameof(GetMyLeavesAsync_PositiveTests)} - Instance 0 - User with RoleType Employee")]
        [DataRow(6, DisplayName = $"{nameof(GetMyLeavesAsync_PositiveTests)} - Instance 1 - User with RoleType Manager")]
        [DataRow(7, DisplayName = $"{nameof(GetMyLeavesAsync_PositiveTests)} - Instance 2 - User with RoleType Admin")]
        public async Task GetMyLeavesAsync_PositiveTests(int instance)
        {
            TestContext.Write("Arrange");
            var leaveService = GetMockLeaveService(instance);

            TestContext.Write("Act");
            var result = await leaveService.GetMyLeavesAsync(new QueryDto(), CancellationToken.None);

            TestContext.Write("Assert");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        [DataRow(3, DisplayName = $"{nameof(GetMyLeavesAsync_NegativeTests)} - Instance 3 - User with RoleType Manager when leaves list is empty")]
        [DataRow(4, DisplayName = $"{nameof(GetMyLeavesAsync_NegativeTests)} - Instance 4 - User with RoleType Admin when leaves is null")]
        [DataRow(5, DisplayName = $"{nameof(GetMyLeavesAsync_NegativeTests)} - Instance 5 - User with RoleType Manager when leaves list contains null")]
        public async Task GetMyLeavesAsync_NegativeTests(int instance)
        {
            TestContext.Write("Arrange");
            var leaveService = GetMockLeaveService(instance);

            TestContext.Write("Act and Assert");
            _ = await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () => await leaveService.GetMyLeavesAsync(new QueryDto(), CancellationToken.None));
        }

        #endregion GetMyLeavesAsync Tests

        #region GetLeaveByIdAsync Tests

        [TestMethod]
        [DataRow(0, DisplayName = $"{nameof(GetLeaveByIdAsync_PositiveTests)} - Instance 0 - With Valid Leave Data")]
        public async Task GetLeaveByIdAsync_PositiveTests(int instance)
        {
            TestContext.Write("Arrange");
            var leaveService = GetMockLeaveService(instance);
            var leaveId = new Guid("7690cb6c-4007-495b-af77-dd3a810e06c2");

            TestContext.Write("Act");
            var result = await leaveService.GetLeaveByIdAsync(leaveId, CancellationToken.None);

            TestContext.Write("Assert");
            Assert.IsNotNull(result);
            Assert.AreEqual(leaveId, result.LeaveId);
        }

        [TestMethod]
        [DataRow(6, DisplayName = $"{nameof(GetLeaveByIdAsync_NegativeTests)} - Instance 1 - With Invalid Leave Data")]
        [DataRow(7, DisplayName = $"{nameof(GetLeaveByIdAsync_NegativeTests)} - Instance 1 - With Empty Leave ID")]
        public async Task GetLeaveByIdAsync_NegativeTests(int instance)
        {
            TestContext.Write("Arrange");
            var leaveService = GetMockLeaveService(instance);
            var leaveId = instance switch
            {
                6 => new Guid("2d9c8e1e-5b3a-4c8f-9b2a-1f0e5a7b8c9d"),
                _ => Guid.Empty
            };

            TestContext.Write("Act and Assert");
            _ = await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () => await leaveService.GetLeaveByIdAsync(leaveId, CancellationToken.None));
        }

        #endregion GetLeaveByIdAsync Tests

        #region ApplyLeaveAsync Tests

        [TestMethod]
        [DataRow(0, DisplayName = $"{nameof(ApplyLeaveAsync_PositiveTests)} - Instance 0 - User with RoleType Employee")]
        [DataRow(6, DisplayName = $"{nameof(ApplyLeaveAsync_PositiveTests)} - Instance 1 - User with RoleType Manager")]
        [DataRow(7, DisplayName = $"{nameof(ApplyLeaveAsync_PositiveTests)} - Instance 2 - User with RoleType Admin")]
        public async Task ApplyLeaveAsync_PositiveTests(int instance)
        {
            TestContext.Write("Arrange");
            var leaveService = GetMockLeaveService(instance);
            var leaveRequestDto = new ApplyLeaveRequestDto
            {
                StartDate = GetUtcDateTime(2023, 10, 1),
                EndDate = GetUtcDateTime(2023, 10, 2),
                Reason = ReasonType.SickLeave
            };

            TestContext.Write("Act");
            var result = await leaveService.ApplyLeaveAsync(leaveRequestDto, CancellationToken.None);

            TestContext.Write("Assert");
            // Use FluentAssertions for more expressive assertions
            result.Should().NotBeNull()
                   .And.BeEquivalentTo(leaveRequestDto, options => options
                                                        .Including(x => x.StartDate)
                                                        .Including(x => x.EndDate)
                                                        .Including(x => x.Reason));
        }

        [TestMethod]
        [DataRow(3, DisplayName = $"{nameof(ApplyLeaveAsync_NegativeTests)} - Instance 3 - User with RoleType Manager when leaves list is empty")]
        [DataRow(4, DisplayName = $"{nameof(ApplyLeaveAsync_NegativeTests)} - Instance 4 - User with RoleType Admin when leaves is null")]
        [DataRow(5, DisplayName = $"{nameof(ApplyLeaveAsync_NegativeTests)} - Instance 5 - User with RoleType Manager when leaves list contains null")]
        public async Task ApplyLeaveAsync_NegativeTests(int instance)
        {
            TestContext.Write("Arrange");
            var leaveService = GetMockLeaveService(instance);
            var leaveRequestDto = new ApplyLeaveRequestDto
            {
                StartDate = GetUtcDateTime(2023, 10, 1),
                EndDate = GetUtcDateTime(2023, 10, 2),
                Reason = ReasonType.SickLeave
            };

            TestContext.Write("Act and Assert");
            _ = await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () => await leaveService.ApplyLeaveAsync(leaveRequestDto, CancellationToken.None));
        }

        #endregion ApplyLeaveAsync Tests

        #region UpdateLeaveAsync Tests

        [TestMethod]
        [DataRow(6, DisplayName = $"{nameof(UpdateLeaveAsync_PositiveTests)} - Instance 1 - User with RoleType Manager")]
        public async Task UpdateLeaveAsync_PositiveTests(int instance)
        {
            TestContext.Write("Arrange");
            var leaveService = GetMockLeaveService(instance);
            var leaveId = new Guid($"769{instance % 2}cb6c-40{instance % 3}7-495b-af77-dd3a810e{instance % 4}6c2");
            var leaveRequestDto = new UpdateLeaveRequestDto
            {
                StartDate = GetUtcDateTime(2023, 10, 1),
                EndDate = GetUtcDateTime(2023, 10, 2),
                Reason = ReasonType.VacationLeave,
                Status = StatusType.Pending
            };

            TestContext.Write("Act");
            var result = await leaveService.UpdateLeaveAsync(leaveId, leaveRequestDto, CancellationToken.None);

            TestContext.Write("Assert");
            result.Should().NotBeNull()
                   .And.BeEquivalentTo(leaveRequestDto, options => options
                                                        .Including(x => x.StartDate)
                                                        .Including(x => x.EndDate)
                                                        .Including(x => x.Reason)
                                                        .Including(x => x.Status));
        }

        [TestMethod]
        [DataRow(3, DisplayName = $"{nameof(UpdateLeaveAsync_NegativeTests)} - Instance 3 - User with RoleType Manager when leaves list is empty")]
        [DataRow(4, DisplayName = $"{nameof(UpdateLeaveAsync_NegativeTests)} - Instance 4 - User with RoleType Admin when leaves is null")]
        public async Task UpdateLeaveAsync_NegativeTests(int instance)
        {
            TestContext.Write("Arrange");
            var leaveService = GetMockLeaveService(instance);
            var leaveId = new Guid($"769{instance % 2}cb6c-40{instance % 3}7-495b-af77-dd3a810e{instance % 4}6c2");
            var leaveRequestDto = new UpdateLeaveRequestDto
            {
                StartDate = GetUtcDateTime(2023, 10, 1),
                EndDate = GetUtcDateTime(2023, 10, 2),
                Reason = ReasonType.VacationLeave,
                Status = StatusType.Approved
            };

            TestContext.Write("Act and Assert");
            _ = await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () => await leaveService.UpdateLeaveAsync(leaveId, leaveRequestDto, CancellationToken.None));
        }

        #endregion UpdateLeaveAsync Tests

        #region DeleteLeaveAsync Tests

        [TestMethod]
        [DataRow(6, DisplayName = $"{nameof(DeleteLeaveAsync_PositiveTests)} - Instance 1 - User with RoleType Manager")]
        public async Task DeleteLeaveAsync_PositiveTests(int instance)
        {
            TestContext.Write("Arrange");
            var leaveService = GetMockLeaveService(instance);
            var leaveId = new Guid($"769{instance % 2}cb6c-40{instance % 3}7-495b-af77-dd3a810e{instance % 4}6c2");

            TestContext.Write("Act");
            var result = await leaveService.DeleteLeaveAsync(leaveId, CancellationToken.None);

            TestContext.Write("Assert");
            result.Should().BeTrue();
        }

        [TestMethod]
        [DataRow(3, DisplayName = $"{nameof(DeleteLeaveAsync_NegativeTests)} - Instance 3 - User with RoleType Manager when leaves list is empty")]
        [DataRow(4, DisplayName = $"{nameof(DeleteLeaveAsync_NegativeTests)} - Instance 4 - User with RoleType Admin when leaves is null")]
        public async Task DeleteLeaveAsync_NegativeTests(int instance)
        {
            TestContext.Write("Arrange");
            var leaveService = GetMockLeaveService(instance);
            var leaveId = new Guid($"769{instance % 2}cb6c-40{instance % 3}7-495b-af77-dd3a810e{instance % 4}6c2");

            TestContext.Write("Act and Assert");
            _ = await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () => await leaveService.DeleteLeaveAsync(leaveId, CancellationToken.None));
        }

        #endregion DeleteLeaveAsync Tests

        #endregion Test Methods
    }
}
