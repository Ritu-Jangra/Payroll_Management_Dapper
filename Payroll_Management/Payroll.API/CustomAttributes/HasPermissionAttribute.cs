namespace Payroll.API
{
    using Microsoft.AspNetCore.Authorization;
    using Payroll.Domain;

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class HasPermissionAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// HasPermissionAttribute is a custom authorization attribute that checks if the user has the specified permission.
        /// </summary>
        /// <param name="permission">Permission to check</param>
        public HasPermissionAttribute(PermissionType permission)
        {
            Policy = Enum.GetName(permission);
        }
    }
}