namespace SML.Display.Tests.Unit.Services;

using Core.Data.Settings;
using Core.Interfaces.Handlers;
using Core.Interfaces.Services;
using Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;

[TestClass]
public class DatabaseAccessCheckerServiceTest
{
    private const string KillExceptionMessage = "Killed";
    private const string KillMessage = "Maximum attempts have been reached for Postgres connection retries!";

    private Mock<IDatabaseAccessChecker> _mockDatabaseAccessChecker = null!;
    private Mock<IProcessKiller> _mockProcessKiller = null!;

    private IDatabaseAccessCheckerService _databaseAccessCheckerService = null!;

    private PostgresReconnectionSettings postgresReconnectionSettings = null!;

    private CancellationTokenSource _cancellationTokenSource = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockDatabaseAccessChecker = new();
        _mockProcessKiller = new();

        postgresReconnectionSettings = new()
        {
            NumberOfRetriesBeforeKill = 3,
            RetryInterval = TimeSpan.FromMicroseconds(10)
        };

        _databaseAccessCheckerService = new DatabaseAccessCheckerService(
            Mock.Of<ILogger<DatabaseAccessCheckerService>>(),
            _mockDatabaseAccessChecker.Object,
            _mockProcessKiller.Object,
            Options.Create(postgresReconnectionSettings));

        _mockProcessKiller.Setup(mock => mock.Kill(It.IsAny<string>()))
            .Throws(new ApplicationException(KillExceptionMessage));

        _cancellationTokenSource = new();
    }

    [TestMethod]
    public async Task TestCheckDatabaseAccessibleAsync()
    {
        // Arrange
        _mockDatabaseAccessChecker.Setup(mock => mock.IsDatabaseAccessible(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true));

        // Act
        await _databaseAccessCheckerService.CheckDatabaseAccessibleAsync(_cancellationTokenSource.Token);

        _mockDatabaseAccessChecker.Verify(mock => mock.IsDatabaseAccessible(It.IsAny<CancellationToken>()), Times.Once);
        _mockDatabaseAccessChecker.Verify(mock => mock.IsDatabaseAccessible(_cancellationTokenSource.Token), Times.Once);
        _mockProcessKiller.Verify(mock => mock.Kill(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task TestCheckDatabaseAccessibleLimitAsync()
    {
        // Arrange
        _mockDatabaseAccessChecker.SetupSequence(mock => mock.IsDatabaseAccessible(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(false))
            .Returns(Task.FromResult(false))
            .Returns(Task.FromResult(false))
            .Returns(Task.FromResult(true));
        var attempts = 1 + postgresReconnectionSettings.NumberOfRetriesBeforeKill;

        // Act
        await _databaseAccessCheckerService.CheckDatabaseAccessibleAsync(_cancellationTokenSource.Token);

        _mockDatabaseAccessChecker.Verify(mock => mock.IsDatabaseAccessible(It.IsAny<CancellationToken>()), Times.Exactly(attempts));
        _mockDatabaseAccessChecker.Verify(mock => mock.IsDatabaseAccessible(_cancellationTokenSource.Token), Times.Exactly(attempts));
        _mockProcessKiller.Verify(mock => mock.Kill(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task TestCheckDatabaseAccessibleKillAsync()
    {
        // Arrange
        _mockDatabaseAccessChecker.SetupSequence(mock => mock.IsDatabaseAccessible(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(false))
            .Returns(Task.FromResult(false))
            .Returns(Task.FromResult(false))
            .Returns(Task.FromResult(false))
            .Returns(Task.FromResult(true));
        var attempts = 1 + postgresReconnectionSettings.NumberOfRetriesBeforeKill;

        try
        {
            // Act
            await _databaseAccessCheckerService.CheckDatabaseAccessibleAsync(_cancellationTokenSource.Token);

            // Assert
            Assert.Fail("No expected kill");
        }
        catch(ApplicationException e)
        {
            Assert.AreEqual(KillExceptionMessage, e.Message);
        }
        _mockDatabaseAccessChecker.Verify(mock => mock.IsDatabaseAccessible(It.IsAny<CancellationToken>()), Times.Exactly(attempts));
        _mockDatabaseAccessChecker.Verify(mock => mock.IsDatabaseAccessible(_cancellationTokenSource.Token), Times.Exactly(attempts));
        _mockProcessKiller.Verify(mock => mock.Kill(It.IsAny<string>()), Times.Once);
        _mockProcessKiller.Verify(mock => mock.Kill(KillMessage), Times.Once);
    }
}
