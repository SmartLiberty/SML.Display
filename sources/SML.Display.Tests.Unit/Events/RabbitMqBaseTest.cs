namespace SML.Display.Tests.Unit.Events;

using Core.Data.Settings;
using Core.Events;
using Core.Interfaces.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitMQ.Client;
using System.Threading;

[TestClass]
public class RabbitMqBaseTest
{
	private const string ExchangeName = "Exchange";

	private Mock<IRabbitMqChannelFactory> _mockRabbitMqChannelFactory = null!;
	private List<Mock<IModel>> _mockChannels = null!;

	private RabbitMqSettings _settings = null!;

	private RabbitMq _rabbitMq = null!;

	private CancellationTokenSource _cancellationTokenSource = null!;

	[TestInitialize]
    public void TestInitialize()
    {
		_mockRabbitMqChannelFactory = new();
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
			BackoffMultiplier = 0
		};

		_rabbitMq = new RabbitMq(_mockRabbitMqChannelFactory.Object, _settings, ExchangeName);

		_cancellationTokenSource = new();

		_mockRabbitMqChannelFactory.Setup(x => x.CreateChannelAsync(It.IsAny<CancellationToken>()))
			.Returns(() =>
			{
				var mockChannel = new Mock<IModel>();
				_mockChannels.Add(mockChannel);
				return Task.FromResult(mockChannel.Object);
			});
	}

	private class RabbitMq : RabbitMqBase
	{
		public IList<IModel> OpenChannels { get; }
		
		public int CloseCount { get; set; }

		public RabbitMq(IRabbitMqChannelFactory channelFactory,
			RabbitMqSettings settings,
			string exchangeName)
			: base(channelFactory, settings, exchangeName)
		{
			OpenChannels = new List<IModel>();
			CloseCount = 0;
		}

		public IModel? GetChannel()
			=> Channel;

		protected override void Initialize(IModel channel)
			=> OpenChannels.Add(channel);

		protected override void End()
			=> CloseCount++;
    }

	[TestMethod]
    public async Task TestStartAsync()
	{
		// Arrange
		var cancellationToken = _cancellationTokenSource.Token;

		// Act
		await _rabbitMq.StartAsync(cancellationToken);

		// Assert
		_mockRabbitMqChannelFactory.Verify(x => x.CreateChannelAsync(It.IsAny<CancellationToken>()), Times.Once);
		_mockRabbitMqChannelFactory.Verify(x => x.CreateChannelAsync(cancellationToken), Times.Once);
		Assert.AreEqual(1, _rabbitMq.OpenChannels.Count());
		Assert.AreEqual(1, _mockChannels.Count());
		Assert.AreEqual(_mockChannels.First().Object, _rabbitMq.OpenChannels.First());
		Assert.AreEqual(_mockChannels.First().Object, _rabbitMq.GetChannel());
        _mockChannels.First().Verify(x => x.Close(), Times.Never);
        Assert.AreEqual(0, _rabbitMq.CloseCount);
	}

	[TestMethod]
    public async Task TestDoubleStartAsync()
	{
		// Arrange
		var cancellationToken = _cancellationTokenSource.Token;
		await _rabbitMq.StartAsync(cancellationToken);
		_mockRabbitMqChannelFactory.Invocations.Clear();
		_rabbitMq.OpenChannels.Clear();

		// Act
		await _rabbitMq.StartAsync(cancellationToken);

		// Assert
		_mockRabbitMqChannelFactory.Verify(x => x.CreateChannelAsync(It.IsAny<CancellationToken>()), Times.Never);
		Assert.AreEqual(0, _rabbitMq.OpenChannels.Count());
		Assert.AreEqual(1, _mockChannels.Count());
		Assert.AreEqual(_mockChannels.First().Object, _rabbitMq.GetChannel());
        _mockChannels.First().Verify(x => x.Close(), Times.Never);
        Assert.AreEqual(0, _rabbitMq.CloseCount);
	}

	[TestMethod]
	public async Task TestStop()
	{
		// Arrange
		var cancellationToken = _cancellationTokenSource.Token;
		await _rabbitMq.StartAsync(cancellationToken);
		_mockRabbitMqChannelFactory.Invocations.Clear();
		_rabbitMq.OpenChannels.Clear();

		// Act
		_rabbitMq.Stop();

		// Assert
		Assert.AreEqual(1, _mockChannels.Count());
		Assert.IsNull(_rabbitMq.GetChannel());
        _mockChannels.First().Verify(x => x.Close(), Times.Once);
        _mockRabbitMqChannelFactory.Verify(x => x.CreateChannelAsync(It.IsAny<CancellationToken>()), Times.Never);
		Assert.AreEqual(0, _rabbitMq.OpenChannels.Count());
		Assert.AreEqual(1, _rabbitMq.CloseCount);
	}

	[TestMethod]
	public async Task TestDoubleStop()
	{
		// Arrange
		var cancellationToken = _cancellationTokenSource.Token;
		await _rabbitMq.StartAsync(cancellationToken);
		_rabbitMq.Stop();
        _mockChannels.First().Invocations.Clear();
		_mockRabbitMqChannelFactory.Invocations.Clear();
		_rabbitMq.OpenChannels.Clear();
		_rabbitMq.CloseCount = 0;

		// Act
		_rabbitMq.Stop();

		// Assert
		Assert.AreEqual(1, _mockChannels.Count());
		Assert.IsNull(_rabbitMq.GetChannel());
        _mockChannels.First().Verify(x => x.Close(), Times.Never);
        _mockRabbitMqChannelFactory.Verify(x => x.CreateChannelAsync(It.IsAny<CancellationToken>()), Times.Never);
		Assert.AreEqual(0, _rabbitMq.OpenChannels.Count());
		Assert.AreEqual(0, _rabbitMq.CloseCount);
	}

	[TestMethod]
    public void TestStopBeforeStart()
	{
		// Act
		_rabbitMq.Stop();

		// Assert
		_mockRabbitMqChannelFactory.Verify(x => x.CreateChannelAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.AreEqual(0, _rabbitMq.OpenChannels.Count());
		Assert.IsNull(_rabbitMq.GetChannel());
		Assert.AreEqual(0, _mockChannels.Count());
		Assert.AreEqual(0, _rabbitMq.CloseCount);
	}
}
