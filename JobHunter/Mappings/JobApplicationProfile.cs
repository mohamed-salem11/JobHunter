using AutoMapper;
using JobHunter.Models;
using JobHunter.ViewModels;

namespace JobHunter.Mappings
{
    public class JobApplicationProfile : Profile
    {
        public JobApplicationProfile()
        {
            CreateMap<JobApplication, CvScoreItem>()
                .ForMember(dest => dest.ApplicantName, opt => opt.MapFrom(src => src.Applicant.FullName))
                .ForMember(dest => dest.ResumeFilePath, opt => opt.MapFrom(src => src.ResumeFilePath));
        }
    }
}
