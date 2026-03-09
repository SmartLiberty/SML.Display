namespace SML.ExampleGrpc.Tests.Unit.MappingProfiles;

using AutoMapper;
using AutoMapper.Internal;
using Core.MappingProfiles;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class MappingProfileConfigurationTest
{
    [TestMethod]
    public void AssertConfigurationIsValid()
    {
        var mapper = new Mapper(new MapperConfiguration(cfg =>
        {
            cfg.Internal().MethodMappingEnabled = false;
            cfg.AddMaps(
                typeof(TimeMappingProfile)
            );
        }, LoggerFactory.Create(b => b.AddConsole())));
        mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }
}