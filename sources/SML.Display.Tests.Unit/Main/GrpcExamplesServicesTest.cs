namespace SML.Display.Tests.Unit.Main;

using AutoMapper;
using Core.Database;
using Core.Main;
using Core.MappingProfiles;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shared.Proto;
using System;
using Tests.Unit.Utils;

[TestClass]
public class GrpcExamplesServicesTest
{
    private FakeTimeProvider _fakeTimeProvider = null!;

    private IMapper _mapper = null!;

    private DateTime _dateTime;

    [TestInitialize]
    public void TestInitialize()
    {
        _fakeTimeProvider = new();

        _dateTime = new DateTime(2023, 06, 14, 15, 30, 0, DateTimeKind.Utc);

        _mapper = new MapperConfiguration(cfg => cfg.AddMaps(typeof(TimeMappingProfile)), LoggerFactory.Create(b => b.AddConsole())).CreateMapper();

        _fakeTimeProvider.SetUtcNow(new(_dateTime));
    }

    [TestMethod]
    public async Task TestUpdateExampleDisplayName()
    {
        // Arrange
        var builder = CreateBuilder(nameof(TestContext.TestName));
        var dbContextOptions = builder.Options;

        using var dbContext = new DatabaseContext(dbContextOptions);

        dbContext.Examples.Add(new()
        {
            DisplayName = "First example"
        });
        dbContext.Examples.Add(new()
        {
            DisplayName = "Last example"
        });
        dbContext.SaveChanges();

        var grpcExamplesServices = new GrpcExamplesServices(Mock.Of<ILogger<GrpcExamplesServices>>(), dbContext, _mapper);

        // Act
        var response = await grpcExamplesServices.Read(new GrpcIdRequest { Id = 2 }, Mock.Of<ServerCallContext>());

        // Assert
        Assert.AreEqual(2, response.Example.Id);
        Assert.AreEqual("Last example", response.Example.DisplayName);
    }

    private DbContextOptionsBuilder<DatabaseContext> CreateBuilder(string methodName)
        => InMemoryDatabaseOptionFactory.Create<DatabaseContext>(methodName, _fakeTimeProvider);
}
