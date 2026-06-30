namespace Payroll.Business
{
    using AutoMapper;
    using Payroll.Contracts;
    using Payroll.Domain;

    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            #region Leave Mappings

            CreateMap<Leave, LeaveResponseDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => GetFullName(src.Employee)))
                .ForMember(dest => dest.ActionByName, opt => opt.MapFrom(src => src.Status == StatusType.Pending ? GetFullName(src.Employee!.Manager) : GetFullName(src.Approver)));

            CreateMap<ApplyLeaveRequestDto, Leave>()
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.EmployeeId, opt => opt.Ignore())
                .ForMember(dest => dest.ActionBy, opt => opt.Ignore());

            CreateMap<UpdateLeaveRequestDto, Leave>()
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.LeaveId, opt => opt.Ignore())
                .ForMember(dest => dest.EmployeeId, opt => opt.Ignore())
                .ForMember(dest => dest.ActionBy, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            #endregion Leave Mappings

            #region Employee Mappings

            CreateMap<Employee, EmployeeResponseDto>()
                .ForMember(dest => dest.ManagerName, opt => opt.MapFrom(src => GetFullName(src.Manager)))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role!.Name));

            CreateMap<AddEmployeeRequestDto, Employee>()
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.Manager, opt => opt.Ignore())
                .ForMember(dest => dest.TeamMembers, opt => opt.Ignore());

            CreateMap<UpdateEmployeeRequestDto, Employee>()
                .ForMember(dest => dest.FirstName, opt => opt.Ignore())
                .ForMember(dest => dest.UserName, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.EmployeeId, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.Manager, opt => opt.Ignore())
                .ForMember(dest => dest.TeamMembers, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            #endregion Employee Mappings
        }

        #region Private Helper Methods

        private static string? GetFullName(Employee? employee)
        {
            return employee != null ? $"{employee.FirstName} {employee.LastName}" : null;
        }

        #endregion Private Helper Methods
    }
}
