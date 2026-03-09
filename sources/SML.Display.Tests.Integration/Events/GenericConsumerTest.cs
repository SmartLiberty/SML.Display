namespace SML.Display.Tests.Integration.Events;

using Core.Data.Settings;
using Core.Events;
using Core.Interfaces.Events;
using Example.Shared.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;

[TestClass]
public class GenericConsumerTest
{
	private static RabbitMqSettings RabbitMqSettings = null!;
	private static IRabbitMqChannelFactory ChannelFactory = null!;

	[ClassInitialize]
	public static void ClassInitialize(TestContext _)
	{
		RabbitMqSettings = Config.InitConfiguration().GetSection(nameof(RabbitMqSettings)).Get<RabbitMqSettings>();
		ChannelFactory = new RabbitMqChannelFactory(new NullLogger<RabbitMqChannelFactory>(), new ConnectionFactory
		{
			HostName = RabbitMqSettings.HostName,
			Port = RabbitMqSettings.Port,
			UserName = RabbitMqSettings.UserName,
			Password = RabbitMqSettings.Password
		}, new TestFailer(), RabbitMqSettings);
	}

	[TestMethod]
	public async Task TestConsumeAsync()
	{
		// Arrange
		var dtoEvent = new DtoExampleEvent
		{
			Id = 45,
			DisplayName = "Example"
		};

		var producer = new GenericProducer<DtoExampleEvent>(new NullLogger<GenericProducer<DtoExampleEvent>>(), ChannelFactory, RabbitMqSettings);
		var consumer = new GenericConsumer<DtoExampleEvent>(new NullLogger<GenericConsumer<DtoExampleEvent>>(), ChannelFactory, RabbitMqSettings);

		var cancellationTokenSource = new CancellationTokenSource();
		var cancellationToken = cancellationTokenSource.Token;

		DtoExampleEvent? consumedEvent = null;
		var consumed = new ManualResetEvent(false);
		consumer.ConsumedEvent += (consumedDtoEvent) =>
		{
			consumedEvent = consumedDtoEvent;
			consumed.Set();
		};

		await producer.StartAsync(cancellationToken);
		await consumer.StartAsync(cancellationToken);

		// Act
		await producer.PublishAsync(dtoEvent);

		// Assert
		Assert.IsTrue(consumed.WaitOne(3000));
		Assert.IsNotNull(consumedEvent);
		Assert.AreEqual(dtoEvent.Id, consumedEvent!.Id);
		Assert.AreEqual(dtoEvent.DisplayName, consumedEvent.DisplayName);

		// Finalize
		consumer.Stop();
		producer.Stop();

		var channel = ChannelFactory.CreateChannelAsync(cancellationToken).Result;
		channel.ExchangeDelete(consumer.ExchangeName);
		channel.Close();
	}

	[TestMethod]
	public async Task TestRoutingKey()
	{
		// Arrange
		var dtoEvent = new DtoExampleEvent
		{
			Id = 45,
			DisplayName = "Example",
		};

		var producer = new GenericProducer<DtoExampleEvent>(new NullLogger<GenericProducer<DtoExampleEvent>>(), ChannelFactory, RabbitMqSettings, DtoExampleEventRoutingKeyHelper.CreateProducerKey);
		var consumer = new GenericConsumer<DtoExampleEvent>(new NullLogger<GenericConsumer<DtoExampleEvent>>(), ChannelFactory, RabbitMqSettings, DtoExampleEventRoutingKeyHelper.CreateConsumerKey(displayName: "Other"));

		var cancellationTokenSource = new CancellationTokenSource();
		var cancellationToken = cancellationTokenSource.Token;

		DtoExampleEvent? consumedEvent = null;
		var consumed = new ManualResetEvent(false);
		consumer.ConsumedEvent += (consumedDtoEvent) =>
		{
			consumedEvent = consumedDtoEvent;
			consumed.Set();
		};

		await producer.StartAsync(cancellationToken);
		await consumer.StartAsync(cancellationToken);

		// Act
		await producer.PublishAsync(dtoEvent);

		// Assert
		Assert.IsFalse(consumed.WaitOne(10));
		Assert.IsNull(consumedEvent);

		// Finalize
		consumer.Stop();
		producer.Stop();

		var channel = ChannelFactory.CreateChannelAsync(cancellationToken).Result;
		channel.ExchangeDelete(consumer.ExchangeName);
		channel.Close();
	}

	[TestMethod]
	public async Task TestRoutingKeys()
	{
		// Arrange
		var dtoEvent = new DtoExampleEvent
		{
			Id = 45,
			DisplayName = "Example",
		};

		var producer = new GenericProducer<DtoExampleEvent>(new NullLogger<GenericProducer<DtoExampleEvent>>(), ChannelFactory, RabbitMqSettings, DtoExampleEventRoutingKeyHelper.CreateProducerKey);
		var consumer = new GenericConsumer<DtoExampleEvent>(new NullLogger<GenericConsumer<DtoExampleEvent>>(), ChannelFactory, RabbitMqSettings, new string[]
		{
			DtoExampleEventRoutingKeyHelper.CreateConsumerKey(dtoEvent.Id, "Other"),
			DtoExampleEventRoutingKeyHelper.CreateConsumerKey(46, dtoEvent.DisplayName),
			DtoExampleEventRoutingKeyHelper.CreateConsumerKey(dtoEvent.Id, dtoEvent.DisplayName),
			DtoExampleEventRoutingKeyHelper.CreateConsumerKey(46, "Other")
		});

		var cancellationTokenSource = new CancellationTokenSource();
		var cancellationToken = cancellationTokenSource.Token;

		DtoExampleEvent? consumedEvent = null;
		var consumed = new ManualResetEvent(false);
		consumer.ConsumedEvent += (consumedDtoEvent) =>
		{
			consumedEvent = consumedDtoEvent;
			consumed.Set();
		};

		await producer.StartAsync(cancellationToken);
		await consumer.StartAsync(cancellationToken);

		// Act
		await producer.PublishAsync(dtoEvent);

		// Assert
		Assert.IsTrue(consumed.WaitOne(3000));
		Assert.IsNotNull(consumedEvent);
		Assert.AreEqual(dtoEvent.Id, consumedEvent!.Id);
		Assert.AreEqual(dtoEvent.DisplayName, consumedEvent.DisplayName);

		// Finalize
		consumer.Stop();
		producer.Stop();

		var channel = ChannelFactory.CreateChannelAsync(cancellationToken).Result;
		channel.ExchangeDelete(consumer.ExchangeName);
		channel.Close();
	}
}
