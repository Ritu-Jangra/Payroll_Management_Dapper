CREATE FUNCTION CheckLeavesOverlap
(
   @EmployeeId UNIQUEIDENTIFIER,
   @StartDate DateTime,
   @EndDate DateTime,
   @ExcludedLeaveId UNIQUEIDENTIFIER = NULL
)
RETURNS Bit
AS
BEGIN
    DECLARE @Result BIT = 0;

    IF EXISTS
    (
        SELECT 1
        FROM Leaves l
        WHERE l.EmployeeId = @EmployeeId
          AND l.Status NOT IN ('Rejected', 'Approved')
          AND (@ExcludedLeaveId IS NULL OR l.LeaveId <> @ExcludedLeaveId)
          AND CAST(@StartDate AS DATE) <= CAST(l.EndDate AS DATE)
		  AND CAST(@EndDate AS DATE) >= CAST(l.StartDate AS DATE)
    )
    BEGIN
        SET @Result = 1;
    END

    RETURN @Result;
END;
GO

Create PROCEDURE dbo.usp_GetEmployeeAndPermissions
    @EmployeeId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        -- Employee Core
        e.*,
        
        -- Role (Splits on RoleId)
        r.RoleId, r.Name,

        -- Manager (Splits on ManagerEmployeeId)
        m.EmployeeId AS ManagerEmployeeId, m.FirstName AS ManagerFirstName, m.LastName AS ManagerLastName,
        m.UserName AS ManagerUserName, m.Email AS ManagerEmail, m.Department AS ManagerDepartment,

        -- Permission (Splits on PermissionId)
        p.PermissionId, p.PermissionName,

        -- Leave (Splits on LeaveId)
        l.LeaveId, l.EmployeeId AS LeaveEmployeeId, l.StartDate, l.EndDate, l.Reason, l.Status
    FROM Employees e
    LEFT JOIN Roles r ON e.RoleId = r.RoleId
    LEFT JOIN Employees m ON e.ManagerId = m.EmployeeId
    LEFT JOIN RolePermissions rp ON r.RoleId = rp.RoleId
    LEFT JOIN Permissions p ON rp.PermissionId = p.PermissionId
    LEFT JOIN Leaves l ON e.EmployeeId = l.EmployeeId
    WHERE e.EmployeeId = @EmployeeId;
END;

Exec usp_GetEmployeeAndPermissions '7487CB5A-7DDD-40AA-6884-08DE9891DBC5'

Create PROCEDURE dbo.usp_GetLeaveDetails
AS
BEGIN
    SET NOCOUNT ON;

   SELECT
    -- LEAVE
    l.LeaveId,
    l.Reason,
    l.StartDate,
    l.EndDate,
    l.Status,
    l.EmployeeId,
    l.ActionBy,

    -- EMPLOYEE
    e.EmployeeId AS EmployeeEmployeeId,
    e.FirstName  AS EmployeeFirstName,
    e.LastName   AS EmployeeLastName,
    e.UserName   AS EmployeeUserName,
    e.Email      AS EmployeeEmail,
    e.Department AS EmployeeDepartment,
    e.RoleId     AS EmployeeRoleId,

    -- MANAGER
    m.EmployeeId AS ManagerEmployeeId,
    m.FirstName  AS ManagerFirstName,
    m.LastName   AS ManagerLastName,
    m.UserName   AS ManagerUserName,
    m.Email      AS ManagerEmail,
    m.Department AS ManagerDepartment,

    -- APPROVER
    a.EmployeeId AS ApproverEmployeeId,
    a.FirstName  AS ApproverFirstName,
    a.LastName   AS ApproverLastName,
    a.UserName   AS ApproverUserName,
    a.Email      AS ApproverEmail,
    a.Department AS ApproverDepartment    
    FROM Leaves l
	LEFT JOIN Employees e
        ON l.EmployeeId = e.EmployeeId
	LEFT JOIN Employees m
        ON e.ManagerId = m.EmployeeId
    LEFT JOIN Employees a
        ON a.EmployeeId = l.ActionBy
END;
Go

CREATE PROCEDURE dbo.usp_GetLeaveDetails
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        l.LeaveId,
        l.Reason,
        l.StartDate,
        l.EndDate,
        l.Status,
        l.EmployeeId,
        l.ActionBy,

        e.EmployeeId,
        e.FirstName,
        e.LastName,
        e.UserName,
        e.Email,
        e.Department,
        e.RoleId,
        e.Salary,
        e.PasswordHash,
        e.ManagerId,

        m.EmployeeId,
        m.FirstName,
        m.LastName,
        m.UserName,
        m.Email,
        m.Department,
        m.Salary,
        m.PasswordHash,

        a.EmployeeId,
        a.FirstName,
        a.LastName,
        a.UserName,
        a.Email,
        a.Department,
        a.Salary,
        a.PasswordHash

    FROM Leaves l
    LEFT JOIN Employees e
        ON l.EmployeeId = e.EmployeeId

    LEFT JOIN Employees m
        ON e.ManagerId = m.EmployeeId

    LEFT JOIN Employees a
        ON l.ActionBy = a.EmployeeId
END;
GO

Exec dbo.usp_GetLeaveDetails