using AutoMapper;
using SurveyBluePrint.Models.DTOs;
using SurveyBluePrint.Models.POCOs;

namespace SurveyBluePrint.Profiles
{
    public class SurveyProfile : Profile
    {
        public SurveyProfile()
        {
            // 🔹 Survey Mapping
            CreateMap<SurveyDto, SurveySchema>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Ignore ID during mapping
                .ReverseMap();

            // 🔹 Section Mapping
            CreateMap<SectionDto, Section>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ReverseMap();

            // 🔹 Question Mapping
            CreateMap<QuestionDto, Question>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ReverseMap();

            // 🔹 Option Mapping
            CreateMap<OptionDto, Option>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ReverseMap();

            // New configuration mappings
            CreateMap<SurveyConfiguration, SurveyConfigurationDto>().ReverseMap();
            CreateMap<ResponseLimit, ResponseLimitDto>().ReverseMap();
            CreateMap<AccessControl, AccessControlDto>().ReverseMap();
            CreateMap<SchedulingConfig, SchedulingConfigDto>().ReverseMap();
            CreateMap<ReminderSettings, ReminderSettingsDto>().ReverseMap();
            CreateMap<UserDetailDto, UserDetails>().ReverseMap();
        }
    }
}
