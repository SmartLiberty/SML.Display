namespace SML.ExampleGrpc.Tests.Unit.Services;

using Core.Data.Settings;
using Core.Data.Storable;
using Core.Database;
using Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using Utils;

[TestClass]
public class DeleteObsoleteArchiveServiceTest
{
    public TestContext TestContext { get; set; } = null!;

    private FakeTimeProvider _fakeTimeProvider = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(new DateTime(2024, 07, 29, 15, 55, 00, DateTimeKind.Utc)));
    }

    [TestMethod]
    public async Task DeleteObsoleteArchive_should_delete_archived_examples_out_of_timespan()
    {
        //Arrange
        var builder = CreateBuilder(nameof(TestContext.TestName));
        using var dbContext = new DatabaseContext(builder.Options);

        dbContext.Examples.Add(new Example
        {
            DisplayName = "Archived and not in time span",
            Archived = true
        });

        dbContext.Examples.Add(new Example
        {
            DisplayName = "Not Archived and in not in time span",
            Archived = false
        });

        // Sessions are saved in two part to set a different LastUpdated value
        dbContext.SaveChanges();

        _fakeTimeProvider.Advance(TimeSpan.FromDays(20));

        dbContext.Examples.Add(new Example
        {
            DisplayName = "Archived and in time span",
            Archived = true
        });

        dbContext.Examples.Add(new Example
        {
            DisplayName = "Not Archived and in time span",
            Archived = false
        });

        dbContext.SaveChanges();

        _fakeTimeProvider.Advance(TimeSpan.FromDays(1));

        IOptions<LifeTimeSettings> lifeTimeSettings = Options.Create(new LifeTimeSettings
        {
            ArchivedExamplesDays = 10
        });

        var obsoleteArchiveCleaner = new DeleteObsoleteArchiveService(dbContext, _fakeTimeProvider, lifeTimeSettings, Mock.Of<ILogger<DeleteObsoleteArchiveService>>());

        //Act
        await obsoleteArchiveCleaner.DeleteObsoleteArchive();

        //Assert
        Assert.AreEqual(3, dbContext.Examples.Count());
    }

    private DbContextOptionsBuilder<DatabaseContext> CreateBuilder(string methodName)
        => InMemoryDatabaseOptionFactory.Create<DatabaseContext>(methodName, _fakeTimeProvider);
}