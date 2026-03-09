namespace SML.Display.Core.MappingProfiles;

using AutoMapper;
using Data.Storable;
using Shared.Proto;

public class ExampleMappingProfile : Profile
{
    public ExampleMappingProfile()
    {
        CreateMap<GrpcCreateRequest, Example>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Commentary, opt => opt.Ignore())
            .ForMember(d => d.LastUpdated, opt => opt.Ignore())
            .ForMember(d => d.Archived, opt => opt.Ignore());
        CreateMap<Example, GrpcExample>()
            .ForMember(d => d.DateTime, opt => opt.MapFrom(s => s.LastUpdated));
    }
}
