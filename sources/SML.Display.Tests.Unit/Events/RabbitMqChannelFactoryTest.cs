namespace SML.Display.Tests.Unit.Events;

using Core.Data.Settings;
using Core.Events;
using Core.Interfaces.Events;
using Core.Interfaces.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

[TestClass]
public class RabbitMqChannelFactoryTest
{
    private Mock<IProcessKiller> _mockProcessKiller = null!;
    private Mock<IConnectionFactory> _mockConnectionFactory = null!;
    private Mock<IConnection> _mockConnection = null!;
    private List<Mock<IModel>> _mockChannels = null!;

    private RabbitMqSettings _settings = null!;

    private IRabbitMqChannelFactory _factory = null!;

    private CancellationTokenSource _cancellationTokenSource = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockProcessKiller = new();
        _mockConnectionFactory = new();
        _mockConnection = new();
        _mockChannels = new();

        _settings = new()
        {
            HostName = "HostName",
            Port = 5678,
            UserName = "UserName",
            Password = "Password",
            ExchangeNamesPrefix = "Tests.Unit-",
            MaxAttempts = 27,
            InitialBackoff = TimeSpan.FromSeconds(0),
            MaxBackoff = TimeSpan.FromMinutes(0),
            BackoffMultiplier = 1
        };

        _factory = new RabbitMqChannelFactory(Mock.Of<ILogger<RabbitMqChannelFactory>>(), _mockConnectionFactory.Object, _mockProcessKiller.Object, _settings);

        _cancellationTokenSource = new();

        _mockProcessKiller.Setup(x => x.Kill(It.IsAny<string>()))
            .Throws(new TaskCanceledException("Service killed"));

        _mockConnectionFactory.Setup(x => x.CreateConnection())
            .Returns(_mockConnection.Object);
        _mockConnection.Setup(x => x.CreateModel())
            .Returns(() =>
            {
                var mockChannel = new Mock<IModel>();
                _mockChannels.Add(mockChannel);
                return mockChannel.Object;
            });
    }

    [TestMethod]
    public async Task TestCreateChannelAsync()
    {
        // Act
        var channel = await _factory.CreateChannelAsync(_cancellationTokenSource.Token);

        // Assert
        Assert.IsNotNull(channel);
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
        _mockConnection.Verify(x => x.CreateModel(), Times.Once);
        Assert.AreEqual(1, _mockChannels.Count);
        var mockChannel = _mockChannels.First();
        Assert.AreEqual(mockChannel.Object, channel);
        _mockConnection.VerifyGet(x => x.IsOpen, Times.Never);
        _mockConnection.Verify(x => x.Close(), Times.Never);
        mockChannel.Verify(x => x.Close(), Times.Never);
    }

    [TestMethod]
    public async Task TestSecondCreateChannelAsync()
    {
        // Arrange
        var firstChannel = await _factory.CreateChannelAsync(_cancellationTokenSource.Token);

        // Act
        var secondChannel = await _factory.CreateChannelAsync(_cancellationTokenSource.Token);

        // Assert
        Assert.IsNotNull(firstChannel);
        Assert.IsNotNull(secondChannel);
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
        _mockConnection.Verify(x => x.CreateModel(), Times.Exactly(2));
        Assert.AreEqual(2, _mockChannels.Count);
        var mockFirstChannel = _mockChannels.First();
        Assert.AreEqual(mockFirstChannel.Object, firstChannel);
        var mockSecondChannel = _mockChannels.Last();
        Assert.AreEqual(mockSecondChannel.Object, secondChannel);
        _mockConnection.VerifyGet(x => x.IsOpen, Times.Never);
        _mockConnection.Verify(x => x.Close(), Times.Never);
        mockFirstChannel.Verify(x => x.Close(), Times.Never);
        mockSecondChannel.Verify(x => x.Close(), Times.Never);
    }

    [TestMethod]
    public async Task TestCreateChannelAsyncBrokerUnreachableException()
    {
        // Arrange
        var exceptions = 2;
        _mockConnectionFactory.Setup(x => x.CreateConnection())
            .Returns(() =>
            {
                if (exceptions-- > 0)
                {
                    throw new BrokerUnreachableException(new());
                }
                return _mockConnection.Object;
            });

        // Act
        var channel = await _factory.CreateChannelAsync(_cancellationTokenSource.Token);

        // Assert
        Assert.IsNotNull(channel);
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Exactly(3));
        _mockConnection.Verify(x => x.CreateModel(), Times.Once);
        Assert.AreEqual(1, _mockChannels.Count);
        var mockChannel = _mockChannels.First();
        Assert.AreEqual(mockChannel.Object, channel);
        _mockConnection.VerifyGet(x => x.IsOpen, Times.Never);
        _mockConnection.Verify(x => x.Close(), Times.Never);
        mockChannel.Verify(x => x.Close(), Times.Never);
    }

    [TestMethod]
    public async Task TestCreateChannelAsyncConnectionException()
    {
        // Arrange
        var exceptions = 2;
        _mockConnectionFactory.Setup(x => x.CreateConnection())
            .Returns(() =>
            {
                if (exceptions-- > 0)
                {
                    throw new Exception();
                }
                return _mockConnection.Object;
            });

        // Act
        var channel = await _factory.CreateChannelAsync(_cancellationTokenSource.Token);

        // Assert
        Assert.IsNotNull(channel);
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Exactly(3));
        _mockConnection.Verify(x => x.CreateModel(), Times.Once);
        Assert.AreEqual(1, _mockChannels.Count);
        var mockChannel = _mockChannels.First();
        Assert.AreEqual(mockChannel.Object, channel);
        _mockConnection.VerifyGet(x => x.IsOpen, Times.Never);
        _mockConnection.Verify(x => x.Close(), Times.Never);
        mockChannel.Verify(x => x.Close(), Times.Never);
    }

    [TestMethod]
    public async Task TestNotCreateChannelAsyncBrokerUnreachableException()
    {
        // Arrange
        _mockConnectionFactory.Setup(x => x.CreateConnection())
            .Throws(new BrokerUnreachableException(new()));
        try
        {
            // Act
            await _factory.CreateChannelAsync(_cancellationTokenSource.Token);

            // Assert
            Assert.Fail("Missing exception");
        }
        catch (TaskCanceledException e)
        {
            Assert.AreEqual("Service killed", e.Message);
        }
        _mockProcessKiller.Verify(x => x.Kill(It.IsAny<string>()));
        _mockProcessKiller.Verify(x => x.Kill("Maximum attempts have been reached for RabbitMQ connection!"));
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Exactly(_settings.MaxAttempts));
        _mockConnection.Verify(x => x.CreateModel(), Times.Never);
    }

    [TestMethod]
    public async Task TestCreateChannelAsyncAlreadyClosedException()
    {
        // Arrange
        var exceptions = 2;
        _mockConnection.Setup(x => x.CreateModel())
            .Returns(() =>
            {
                if (exceptions-- > 0)
                {
                    throw new AlreadyClosedException(new(new(), 0, ""));
                }
                var mockChannel = new Mock<IModel>();
                _mockChannels.Add(mockChannel);
                return mockChannel.Object;
            });

        // Act
        var channel = await _factory.CreateChannelAsync(_cancellationTokenSource.Token);

        // Assert
        Assert.IsNotNull(channel);
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
        _mockConnection.Verify(x => x.CreateModel(), Times.Exactly(3));
        Assert.AreEqual(1, _mockChannels.Count);
        var mockChannel = _mockChannels.First();
        Assert.AreEqual(mockChannel.Object, channel);
        _mockConnection.VerifyGet(x => x.IsOpen, Times.Never);
        _mockConnection.Verify(x => x.Close(), Times.Never);
        mockChannel.Verify(x => x.Close(), Times.Never);
    }

    [TestMethod]
    public async Task TestCreateChannelAsyncChannelException()
    {
        // Arrange
        var exceptions = 2;
        _mockConnection.Setup(x => x.CreateModel())
            .Returns(() =>
            {
                if (exceptions-- > 0)
                {
                    throw new Exception();
                }
                var mockChannel = new Mock<IModel>();
                _mockChannels.Add(mockChannel);
                return mockChannel.Object;
            });

        // Act
        var channel = await _factory.CreateChannelAsync(_cancellationTokenSource.Token);

        // Assert
        Assert.IsNotNull(channel);
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
        _mockConnection.Verify(x => x.CreateModel(), Times.Exactly(3));
        Assert.AreEqual(1, _mockChannels.Count);
        var mockChannel = _mockChannels.First();
        Assert.AreEqual(mockChannel.Object, channel);
        _mockConnection.VerifyGet(x => x.IsOpen, Times.Never);
        _mockConnection.Verify(x => x.Close(), Times.Never);
        mockChannel.Verify(x => x.Close(), Times.Never);
    }

    [TestMethod]
    public async Task TestNotCreateChannelAsyncAlreadyClosedException()
    {
        // Arrange
        _mockConnection.Setup(x => x.CreateModel())
            .Throws(new AlreadyClosedException(new(new(), 0, "")));
        try
        {
            // Act
            await _factory.CreateChannelAsync(_cancellationTokenSource.Token);

            // Assert
            Assert.Fail("Missing exception");
        }
        catch (TaskCanceledException e)
        {
            Assert.AreEqual("Service killed", e.Message);
        }
        _mockProcessKiller.Verify(x => x.Kill(It.IsAny<string>()));
        _mockProcessKiller.Verify(x => x.Kill("Maximum attempts have been reached for RabbitMQ channel creation!"));
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
        _mockConnection.Verify(x => x.CreateModel(), Times.Exactly(_settings.MaxAttempts));
    }

    [TestMethod]
    public async Task TestNotCreateChannelCancelBeforeFirstAttempt()
    {
        // Arrange
        _cancellationTokenSource.Cancel();

        try
        {
            // Act
            await _factory.CreateChannelAsync(_cancellationTokenSource.Token);

            // Assert
            Assert.Fail("Missing exception");
        }

        catch (TaskCanceledException e)
        {
            Assert.AreEqual("RabbitMQ channel creation is cancelled", e.Message);
        }

        // Assert
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Never);
        _mockConnection.Verify(x => x.CreateModel(), Times.Never);
        _mockConnection.VerifyGet(x => x.IsOpen, Times.Never);
        _mockConnection.Verify(x => x.Close(), Times.Never);
    }

    [TestMethod]
    public async Task TestNotCreateChannelCancelDuringDelay()
    {
        // Arrange
        _settings.InitialBackoff = TimeSpan.FromHours(1);
        _factory = new RabbitMqChannelFactory(Mock.Of<ILogger<RabbitMqChannelFactory>>(), _mockConnectionFactory.Object, _mockProcessKiller.Object, _settings);
        _mockConnectionFactory.Setup(x => x.CreateConnection())
            .Throws(new BrokerUnreachableException(new()));

        var task = Task.Run(() => _factory.CreateChannelAsync(_cancellationTokenSource.Token));
        await Task.Delay(TimeSpan.FromMilliseconds(100));

        // Act
        _cancellationTokenSource.Cancel();

        // Assert
        try
        {
            Assert.IsTrue(task.Wait(TimeSpan.FromSeconds(5)));
            Assert.Fail("Missing exception");
        }
        catch (AggregateException e)
        {
            Assert.AreEqual(1, e.InnerExceptions.Count);
            Assert.AreEqual("A task was canceled.", e.InnerExceptions.First().Message);
        }
        _mockConnectionFactory.Verify(x => x.CreateConnection(), Times.Once);
        _mockConnection.Verify(x => x.CreateModel(), Times.Never);
    }

    [TestMethod]
    public async Task TestShutdownDirectReconnect()
    {
        // Arrange
        await _factory.CreateChannelAsync(_cancellationTokenSource.Token);
        _mockConnection.SetupGet(x => x.IsOpen)
            .Returns(true);

        // Act
        _mockConnection.Raise(x => x.ConnectionShutdown += null, null!, null!);

        // Assert
        _mockConnection.VerifyGet(x => x.IsOpen, Times.Once);
        _mockConnection.Verify(x => x.Close(), Times.Never);
        _mockChannels.First().Verify(x => x.Close(), Times.Never);
    }

    [TestMethod]
    public async Task TestShutdownReconnect()
    {
        // Arrange
        bool isOpen = true;
        await _factory.CreateChannelAsync(_cancellationTokenSource.Token);
        _mockConnection.SetupGet(x => x.IsOpen)
            .Returns(() =>
            {
                isOpen = !isOpen;
                return isOpen;
            });

        // Act
        _mockConnection.Raise(x => x.ConnectionShutdown += null, null!, null!);

        // Assert
        _mockConnection.VerifyGet(x => x.IsOpen, Times.Exactly(2));
        _mockConnection.Verify(x => x.Close(), Times.Never);
        _mockChannels.First().Verify(x => x.Close(), Times.Never);
    }

    [TestMethod]
    public async Task TestDoubleShutdown()
    {
        // Arrange
        bool isOpen = true;
        await _factory.CreateChannelAsync(_cancellationTokenSource.Token);
        _mockConnection.SetupGet(x => x.IsOpen)
            .Returns(() =>
            {
                if (isOpen)
                {
                    _mockConnection.Raise(x => x.ConnectionShutdown += null, null!, null!);
                }
                isOpen = !isOpen;
                return isOpen;
            });

        // Act
        _mockConnection.Raise(x => x.ConnectionShutdown += null, null!, null!);

        // Assert
        _mockConnection.VerifyGet(x => x.IsOpen, Times.Exactly(2));
        _mockConnection.Verify(x => x.Close(), Times.Never);
        _mockChannels.First().Verify(x => x.Close(), Times.Never);
    }

    [TestMethod]
    public async Task TestShutdownNeverReconnect()
    {
        // Arrange
        await _factory.CreateChannelAsync(_cancellationTokenSource.Token);
        _mockConnection.SetupGet(x => x.IsOpen)
            .Returns(false);
        try
        {
            // Act
            _mockConnection.Raise(x => x.ConnectionShutdown += null, null!, null!);

            // Assert
            Assert.Fail("Missing exception");
        }
        catch (TaskCanceledException e)
        {
            Assert.AreEqual("Service killed", e.Message);
        }
        _mockProcessKiller.Verify(x => x.Kill(It.IsAny<string>()));
        _mockProcessKiller.Verify(x => x.Kill("Maximum attempts have been reached for RabbitMQ reconnection!"));
        _mockConnection.VerifyGet(x => x.IsOpen, Times.Exactly(_settings.MaxAttempts));
        _mockConnection.Verify(x => x.Close(), Times.Never);
        _mockChannels.First().Verify(x => x.Close(), Times.Never);
    }

    [TestMethod]
    public async Task TestShutdownCancel()
    {
        // Arrange
        await _factory.CreateChannelAsync(_cancellationTokenSource.Token);
        _mockConnection.SetupGet(x => x.IsOpen)
            .Returns(() =>
            {
                _factory.Dispose();
                return false;
            });

        try
        {
            // Act
            await Task.Run(() => _mockConnection.Raise(x => x.ConnectionShutdown += null, null!, null!));

            // Assert
            Assert.Fail("Missing exception");
        }
        catch (TaskCanceledException e)
        {
            Assert.AreEqual("RabbitMQ reconnection is cancelled", e.Message);
        }
        _mockConnection.VerifyGet(x => x.IsOpen, Times.Once);
        _mockConnection.Verify(x => x.Close(), Times.Once);
        _mockChannels.First().Verify(x => x.Close(), Times.Never);
    }

    [TestMethod]
    public async Task TestShutdownCancelDuringDelay()
    {
        // Arrange
        _settings.InitialBackoff = TimeSpan.FromHours(1);
        _factory = new RabbitMqChannelFactory(Mock.Of<ILogger<RabbitMqChannelFactory>>(), _mockConnectionFactory.Object, _mockProcessKiller.Object, _settings);
        await _factory.CreateChannelAsync(_cancellationTokenSource.Token);
        _mockConnection.SetupGet(x => x.IsOpen)
            .Returns(false);
        var task = Task.Run(() => _mockConnection.Raise(x => x.ConnectionShutdown += null, null!, null!));
        await Task.Delay(TimeSpan.FromMilliseconds(100));

        // Act
        _factory.Dispose();

        // Assert
        try
        {
            Assert.IsTrue(task.Wait(TimeSpan.FromSeconds(5)));
            Assert.Fail("Missing exception");
        }
        catch (AggregateException e)
        {
            Assert.AreEqual(1, e.InnerExceptions.Count);
            Assert.AreEqual("The operation was canceled.", e.InnerExceptions.First().Message);
        }
        _mockConnection.Verify(x => x.Close(), Times.Once);
        _mockChannels.First().Verify(x => x.Close(), Times.Never);
        _mockConnection.VerifyGet(x => x.IsOpen, Times.Never);
    }

    [TestMethod]
    public void TestIncreaseBackoff()
    {
        // Arrange
        _settings.InitialBackoff = TimeSpan.FromSeconds(1);
        _settings.MaxBackoff = TimeSpan.FromMinutes(1);
        _settings.BackoffMultiplier = 2;
        _factory = new RabbitMqChannelFactory(Mock.Of<ILogger<RabbitMqChannelFactory>>(), _mockConnectionFactory.Object, _mockProcessKiller.Object, _settings);
        var backoff = _settings.InitialBackoff;
        var list = new TimeSpan[7];

        for (var i = 0; i < list.Length; i++)
        {
            // Act
            backoff = _factory.Increase(backoff);

            list[i] = backoff;
        }

        // Assert
        Assert.AreEqual(TimeSpan.FromSeconds(2), list[0]);
        Assert.AreEqual(TimeSpan.FromSeconds(4), list[1]);
        Assert.AreEqual(TimeSpan.FromSeconds(8), list[2]);
        Assert.AreEqual(TimeSpan.FromSeconds(16), list[3]);
        Assert.AreEqual(TimeSpan.FromSeconds(32), list[4]);
        Assert.AreEqual(TimeSpan.FromMinutes(1), list[5]);
        Assert.AreEqual(TimeSpan.FromMinutes(1), list[6]);
    }
}
