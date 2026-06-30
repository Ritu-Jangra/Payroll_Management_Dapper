namespace Payroll.Data
{
    using Dapper;
    using Payroll.Domain;
    using System.Data;
    using System.Data.Common;

    public class EmployeeRepository : BaseRepository, IEmployeeRepository
    {
        private readonly DbConnection _dbConnection;

        public EmployeeRepository(DbConnection dbConnection) : base(dbConnection)
        {
            _dbConnection = dbConnection;
        }

        /// <summary>
        /// Gets all employees with optional filtering by adminId, managerId, or employeeId.
        /// If adminId is provided, the admin user will be excluded from the results.
        /// If managerId is provided, only employees reporting to that manager will be included.
        /// If employeeId is provided, only that specific employee will be included.
        /// </summary>
        /// <param name="adminId">The ID of the admin user</param>
        /// <param name="managerId">The ID of the manager</param>
        /// <param name="employeeId">The ID of the employee</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns a list of employees</returns>
        public async Task<List<Employee>> GetAllEmployeesAsync(Guid? adminId = null,
                                                               Guid? managerId = null,
                                                               Guid? employeeId = null,
                                                               CancellationToken cancellationToken = default)

        {
            var sql = @"
                        SELECT
                            e.*,
                            m.*,
                            r.*,
                            l.*
                        FROM Employees e
                        LEFT JOIN Employees m ON e.ManagerId = m.EmployeeId
                        LEFT JOIN Roles r ON e.RoleId = r.RoleId
                        LEFT JOIN Leaves l ON l.EmployeeId = e.EmployeeId";

            var employeeDict = new Dictionary<Guid, Employee>();

            await _dbConnection.QueryAsync<Employee, Employee, Role, Leave, Employee>(
                sql,
                (employee, manager, role, leave) =>
                {
                    if (!employeeDict.TryGetValue(employee.EmployeeId, out var emp))
                    {
                        emp = employee;
                        emp.Manager = manager;
                        emp.Role = role;
                        emp.Leaves = new List<Leave>();

                        employeeDict.Add(emp.EmployeeId, emp);
                    }

                    if (leave != null)
                    {
                        emp.Leaves.Add(leave);
                    }

                    return emp;
                },
                splitOn: "EmployeeId,ManagerId,RoleId"
            );

            List<Employee> employees = employeeDict.Values.ToList();

            // Exclude the admin user if adminId is provided
            if (adminId.HasValue)
            {
                employees = employees.Where(e => e.EmployeeId != adminId).ToList();
            }

            // Filter by managerId if provided
            if (managerId.HasValue)
            {
                employees = employees.Where(l => l.ManagerId == managerId.Value).ToList();
            }

            // Filter by employeeId if provided
            if (employeeId.HasValue)
            {
                employees = employees.Where(e => e.EmployeeId == employeeId.Value).ToList();
            }

            return employees;
        }

        /// <summary>
        /// Get an employee by their username, including their role and permissions for authentication purposes.
        /// </summary>
        /// <param name="username">The username of the employee</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the employee if found, otherwise null</returns>
        public async Task<Employee?> GetEmployeeUsernameAsync(string username, CancellationToken cancellationToken)
        {
            Guid employeeId = await GetEmployeeIdByUserNameAsync(username, cancellationToken);
            return await GetEmployeeWithAuthQuery(employeeId);
        }

        /// <summary>
        /// Get an employee by their user ID, including their role and permissions for authentication purposes.
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the employee if found, otherwise null</returns>
        public async Task<Employee?> GetEmployeeWithPermissionsAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await GetEmployeeWithAuthQuery(userId);
        }

        /// <summary>
        /// Get an employee by their ID, including their manager, role, and leaves for detailed information purposes.
        /// </summary>
        /// <param name="employeeId">The ID of the employee</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the employee if found, otherwise null</returns>
        public async Task<Employee?> GetEmployeeByIdAsync(Guid employeeId, DbTransaction transaction, CancellationToken cancellationToken)
        {
            var sql = @"Select * from Employees WHERE EmployeeId = @EmployeeId;";

            return await _dbConnection.QueryFirstOrDefaultAsync<Employee>(sql, new { employeeId }, transaction);
        }

        /// <summary>
        /// Get an employee by their ID, including their manager, role, and leaves for detailed information purposes.
        /// </summary>
        /// <param name="employeeId">The ID of the employee</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the employee if found, otherwise null</returns>
        public async Task<Employee?> GetEmployeeDetailsAsync(Guid employeeId, CancellationToken cancellationToken)
        {
            var sql = @"
                        SELECT
                            e.*,
                            m.*,
                            r.*,
                            l.*
                        FROM Employees e
                        LEFT JOIN Employees m ON e.ManagerId = m.EmployeeId
                        LEFT JOIN Roles r ON e.RoleId = r.RoleId
                        LEFT JOIN Leaves l ON l.EmployeeId = e.EmployeeId
                        WHERE e.EmployeeId = @EmployeeId;";

            Employee? employeeResult = null;

            await _dbConnection.QueryAsync<Employee, Employee, Role, Leave, Employee>(
                sql,
                (employee, manager, role, leave) =>
                {
                    if (employeeResult == null)
                    {
                        employeeResult = employee;
                        employeeResult.Manager = manager;
                        employeeResult.Role = role;
                        employeeResult.Leaves = new List<Leave>();
                    }

                    if (leave != null)
                    {
                        employeeResult.Leaves.Add(leave);
                    }

                    return employee;
                },
                new { employeeId },
                splitOn: "EmployeeId,RoleId,LeaveId");

            return employeeResult;
        }

        /// <summary>
        /// Adds a new employee to the database. 
        /// The employee object should have all required properties set, including the RoleId for the employee's role.
        /// </summary>
        /// <param name="employee">The employee to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the added employee entity</returns>
        public async Task AddEmployeeAsync(Employee employee, DbTransaction transaction, CancellationToken cancellationToken)
        {
            var sql = @"INSERT INTO Employees (EmployeeId, FirstName, LastName, UserName, Email, Department, PasswordHash, Salary, RoleId, ManagerId)
                        VALUES (@EmployeeId, @FirstName, @LastName, @UserName, @Email, @Department, @PasswordHash, @Salary, @RoleId, @ManagerId);
                       ";

            await _dbConnection.ExecuteAsync(sql, new
            {
                employee.EmployeeId,
                employee.FirstName,
                employee.LastName,
                employee.UserName,
                employee.Email,
                Department = employee.Department.ToString(),
                employee.PasswordHash,
                employee.Salary,
                employee.RoleId,
                employee.ManagerId
            },
            transaction);
        }

        /// <summary>
        /// Deletes an existing employee from the database. 
        /// The employee object should have a valid EmployeeId that exists in the database.
        /// </summary>
        /// <param name="employee">The employee to delete</param>
        public async Task DeleteEmployee(Employee employee, DbTransaction transaction)
        {
            await _dbConnection.ExecuteAsync("DELETE FROM Employees WHERE EmployeeId = @EmployeeId", new { employee.EmployeeId }, transaction);
        }

        /// <summary>
        /// Updates an existing employee in the database.
        /// </summary>
        /// <param name="employee">The employee to update</param>
        public async Task UpdateEmployee(Employee employee, DbTransaction transaction)
        {
            var sql = @"UPDATE Employees
                        SET
                            Salary = @Salary,
                            RoleId = @RoleId,
                            ManagerId = @ManagerId
                        WHERE EmployeeId = @EmployeeId;
                        ";

            await _dbConnection.ExecuteAsync(sql, new
            {
                employee.EmployeeId,
                employee.FirstName,
                employee.LastName,
                employee.UserName,
                employee.Email,
                Department = employee.Department.ToString(),
                employee.PasswordHash,
                employee.Salary,
                employee.RoleId,
                employee.ManagerId
            }, transaction);
        }

        /// <summary>
        /// Ensures that the provided username is unique across all employees in the database, ignoring case sensitivity.
        /// </summary>
        /// <param name="userName">Username to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the username exists, otherwise false</returns>
        public async Task<bool> EmployeeUserNameExistsAsync(string userName, DbTransaction? transaction, CancellationToken cancellationToken)
        {
            var sql = @"SELECT COUNT(1)
                        FROM Employees
                        WHERE LOWER(UserName) = LOWER(@UserName)";

            return await _dbConnection.ExecuteScalarAsync<bool>(sql, new { UserName = userName }, transaction);
        }

        /// <summary>
        /// Ensures that the provided email is unique across all employees in the database, ignoring case sensitivity.
        /// </summary>
        /// <param name="email">Email to check</param>
        /// <param name="excludeEmployeeId">Employee ID to exclude from the check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the email exists, otherwise false</returns>
        public async Task<bool> EmployeeEmailExistsAsync(string email, Guid? excludeEmployeeId = null, DbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            var sql = @"SELECT COUNT(1)
                        FROM Employees e
                        WHERE LOWER(Email) = LOWER(@Email) And EmployeeId != @ExcludeEmployeeId";

            return await _dbConnection.ExecuteScalarAsync<bool>(sql, new { Email = email, ExcludeEmployeeId = excludeEmployeeId }, transaction);
        }

        /// <summary>
        /// Ensures that the provided username is unique within the specified department, ignoring case sensitivity.
        /// </summary>
        /// <param name="userName">Username to check</param>
        /// <param name="department">Department to check</param>
        /// <param name="excludeEmployeeId">Employee ID to exclude from the check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the username exists in the department, otherwise false</returns>
        public async Task<bool> EmployeeUserNameExistsInDepartmentAsync(string userName,
                                                                        DepartmentType department,
                                                                        Guid? excludeEmployeeId = null,
                                                                        DbTransaction? transaction = null,
                                                                        CancellationToken cancellationToken = default)
        {
            var sql = @"SELECT COUNT(1)
                        FROM Employees
                        WHERE LOWER(UserName) = LOWER(@UserName) And Department = @Department And EmployeeId != @ExcludeEmployeeId";

            return await _dbConnection.ExecuteScalarAsync<bool>(sql, new { UserName = userName, Department = department.ToString(), ExcludeEmployeeId = excludeEmployeeId }, transaction);
        }

        /// <summary>
        /// Gets the employee ID associated with the provided username, ignoring case sensitivity.
        /// </summary>
        /// <param name="userName">Username to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The employee ID if found, otherwise Guid.Empty</returns>
        public async Task<Guid> GetEmployeeIdByUserNameAsync(string userName, CancellationToken cancellationToken)
        {
            var sql = @"SELECT EmployeeId
                        FROM Employees
                        WHERE LOWER(UserName) = LOWER(@UserName)";

            Employee? employee = await _dbConnection.QueryFirstOrDefaultAsync<Employee>(sql, new { UserName = userName });
            return employee != null ? employee.EmployeeId : Guid.Empty;
        }

        /// <summary>
        /// Gets the role ID associated with the provided role type.
        /// </summary>
        /// <param name="roleType">Role type to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The role ID if found, otherwise Guid.Empty</returns>
        public async Task<Guid> GetRoleIdByRoleTypeAsync(RoleType roleType, DbTransaction transaction, CancellationToken cancellationToken)
        {
            var sql = @"SELECT RoleId
                        FROM Roles
                        WHERE Name = @RoleType";

            Role? role = await _dbConnection.QueryFirstOrDefaultAsync<Role>(sql, new { RoleType = roleType.ToString() }, transaction);
            return role != null ? role.RoleId : Guid.Empty;
        }

        #region Private helper methods

        private async Task<Employee?> GetEmployeeWithAuthQuery(Guid employeeId)
        {
            Employee? employee = null;

            var uniquePermissions = new HashSet<int>();
            var uniqueLeaves = new HashSet<Guid>();

            var parameters = new DynamicParameters();
            parameters.Add("@EmployeeId", employeeId, DbType.Guid);

            await _dbConnection.QueryAsync<Employee, Role, Employee, Permissions, Leave, Employee>(
                "dbo.usp_GetEmployeeAndPermissions",
                (emp, role, mgr, perm, leave) =>
                {
                    if (employee == null)
                    {
                        employee = emp;
                        employee.Role = role;
                        employee.Manager = mgr;
                        employee.Leaves = new List<Leave>();

                        if (employee.Role != null)
                        {
                            employee.Role.RolePermissions = new List<RolePermission>();
                        }
                    }

                    if (perm != null &&
                        employee?.Role != null &&
                        uniquePermissions.Add(perm.PermissionId))
                    {
                        employee.Role.RolePermissions.Add(new RolePermission
                        {
                            RoleId = employee.RoleId,
                            PermissionId = perm.PermissionId,
                            Permission = perm,
                            Role = employee.Role
                        });
                    }

                    if (leave != null &&
                        employee != null &&
                        uniqueLeaves.Add(leave.LeaveId))
                    {
                        employee.Leaves.Add(leave);
                    }

                    return employee!;
                },
                param: parameters,
                splitOn: "RoleId,ManagerEmployeeId,PermissionId,LeaveId",
                commandType: CommandType.StoredProcedure);

            return employee;
        }

        #endregion Private helper methods
    }
}
