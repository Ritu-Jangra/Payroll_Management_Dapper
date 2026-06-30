namespace Payroll.Domain
{
    public enum PermissionType
    {
        // Personal Info
        VIEW_PERSONAL_INFO,

        // Leave
        APPLY_LEAVE,
        UPDATE_LEAVE,
        DELETE_LEAVE,
        APPROVE_LEAVE,
        REJECT_LEAVE,
        VIEW_MY_LEAVES,
        VIEW_TEAM_LEAVES,

        // Team
        VIEW_TEAM,

        // Employee Management
        ADD_EMPLOYEE,
        UPDATE_EMPLOYEE,
        DELETE_EMPLOYEE,
    }
}
