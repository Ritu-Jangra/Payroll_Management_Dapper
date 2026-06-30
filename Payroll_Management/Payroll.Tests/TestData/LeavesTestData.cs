namespace Payroll.Tests
{
    using Payroll.Domain;
    using System;

    internal static class LeavesTestData
    {
        internal static List<Leave?>? GetListOfLeaves(int instance)
        {
            List<Leave?>? leaves = new();

            switch (instance)
            {
                case 1 or 2:
                    for (int i = 0; i <= 7; i++)
                    {
                        leaves.Add(GetLeave(i));
                    }
                    break;
                case 3 or 99:
                    break;
                case 4:
                    leaves = null;
                    break;
                case 5:
                    leaves.Add(null);
                    break;
                default:
                    leaves.Add(GetLeave(instance));
                    break;
            }

            return leaves;
        }

        private static Leave GetLeave(int instance)
        {
            return instance switch
            {
                _ => new Leave
                {
                    LeaveId = new Guid($"769{instance % 2}cb6c-40{instance % 3}7-495b-af77-dd3a810e{instance % 4}6c2"),
                    Reason = GetReasonType(instance),
                    StartDate = GetLeaveDate(instance, true),
                    EndDate = GetLeaveDate(instance, false),
                    Status = GetStatusType(instance),
                    EmployeeId = new Guid($"52{instance % 2}C2653-44DA-{instance % 3}0CB-8590-B924B{instance % 4}765878"),
                    Employee = GetEmployee(instance),
                    ActionBy = instance switch
                    {
                        0 or 1 or 2 => new Guid("521C2653-44DA-10CB-8590-B924B1765878"),
                        3 or 4 or 5 => new Guid("522C2653-44DA-20CB-8590-B924B2765878"),
                        _ => null
                    },
                    Approver = instance switch
                    {
                        0 or 1 or 2 => GetEmployee(1),
                        3 or 4 or 5 => GetEmployee(2),
                        _ => null
                    }
                }
            };
        }

        private static DateTime GetLeaveDate(int instance, bool isStartDate)
        {
            return instance switch
            {
                1 => GetUtcDateTime(2023, 1, isStartDate ? 1 : 5),
                2 => GetUtcDateTime(2023, 1, isStartDate ? 10 : 15),
                4 => GetUtcDateTime(2023, 1, isStartDate ? 3 : 7),
                6 => GetUtcDateTime(2023, 1, isStartDate ? 4 : 8),
                _ => GetUtcDateTime(2023, 1, isStartDate ? 5 : 7)
            };
        }

        private static ReasonType GetReasonType(int instance)
        {
            return instance switch
            {
                1 => ReasonType.CasualLeave,
                2 => ReasonType.VacationLeave,
                4 => ReasonType.PaternityLeave,
                5 => ReasonType.MaternityLeave,
                _ => ReasonType.SickLeave
            };
        }

        private static StatusType GetStatusType(int instance)
        {
            return instance switch
            {
                1 or 4 or 7 => StatusType.Approved,
                2 or 6 => StatusType.Pending,
                _ => StatusType.Rejected
            };
        }

        private static Employee GetEmployee(int instance)
        {
            return new Employee
            {
                EmployeeId = new Guid($"52{instance % 2}C2653-44DA-{instance % 3}0CB-8590-B924B{instance % 4}765878"),
                FirstName = $"Employee{instance}",
                LastName = $"Test{instance}",
                UserName = $"employee{instance}",
                Email = $"employee{instance}@test.com",
                PasswordHash = $"hashedpassword{instance}",
                Department = instance switch
                {
                    0 or 1 or 2 => DepartmentType.IT,
                    3 => DepartmentType.HR,
                    _ => DepartmentType.Finance
                },
                Salary = 20000 + (instance * 1000),
                Role = new Role
                {
                    RoleId = instance switch
                    {
                        0 or 1 or 2 => new Guid($"ff09d{instance % 2}cd-121{instance % 3}-47bb-96b1-f26{instance % 4}aa88f140"),
                        3 or 4 or 5 => new Guid($"ff09d{(instance + 1) % 2}cd-121{(instance + 1) % 3}-47bb-96b1-f26{(instance + 1) % 4}aa88f140"),
                        _ => Guid.Empty
                    },
                    Name = instance switch
                    {
                        0 or 1 or 2 => RoleType.Employee,
                        3 or 4 or 5 => RoleType.Admin,
                        _ => RoleType.Employee
                    },
                },
                ManagerId = instance switch
                {
                    0 or 1 or 2 => new Guid("521C2653-44DA-10CB-8590-B924B1765878"),
                    3 or 4 or 5 => new Guid("522C2653-44DA-20CB-8590-B924B2765878"),
                    6 => new Guid("520C2653-44DA-00CB-8590-B924B2765878"),
                    _ => null
                }
            };
        }

        internal static DateTime GetUtcDateTime(int year, int month, int day)
        {
            return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        }
    }
}
