using AutoMapper;
using OnBoarding.Models.DTOs;
using OnBoarding.Models.POCOs;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // DTO02 -> User
        CreateMap<DTO02, User>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null)); // Ignore null values

        // DTO01 -> Otp
        CreateMap<DTO01, Otp>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null)); // Ignore null values

        CreateMap<DTO01, Otp>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

    }
}
