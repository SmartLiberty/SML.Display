namespace SML.ExampleGrpc.Tests.Integration.Events;

using Core.Data.Settings;
using Core.Events;
using Core.Interfaces.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;
using SML.Example.Shared.Dtos;
using System.Threading;

[TestClass]
public class GenericProducerTest
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
	public async Task TestPublishAsync()
	{
		// Arrange
		var dtoEvent = new DtoExampleEvent
		{
			Id = 45,
			DisplayName = "Example",
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
	public async Task TestPublishConcurrencyAsync()
	{
		// Arrange
		var count = 10;
		var dtoEvent = new DtoExampleEvent
		{
			Id = 45,
			DisplayName = "Example",
		};

		var producer = new GenericProducer<DtoExampleEvent>(new NullLogger<GenericProducer<DtoExampleEvent>>(), ChannelFactory, RabbitMqSettings);
		var consumer = new GenericConsumer<DtoExampleEvent>(new NullLogger<GenericConsumer<DtoExampleEvent>>(), ChannelFactory, RabbitMqSettings);

		var cancellationTokenSource = new CancellationTokenSource();
		var cancellationToken = cancellationTokenSource.Token;

		var consumedEvents = new List<DtoExampleEvent>();
		var consumed = new ManualResetEvent(false);
		consumer.ConsumedEvent += (consumedDtoEvent) =>
		{
            consumedEvents.Add(consumedDtoEvent);
			if (consumedEvents.Count == count)
			{
				consumed.Set();
			}
		};

		await producer.StartAsync(cancellationToken);
		await consumer.StartAsync(cancellationToken);

		for (var i = 1; i < count; i++)
        {
            _ = Task.Run(() => producer.PublishAsync(dtoEvent), cancellationToken);
        }

        // Act
        await producer.PublishAsync(dtoEvent);

		// Assert
		Assert.IsTrue(consumed.WaitOne(3000));
		Assert.IsNotNull(consumedEvents);
		Assert.AreEqual(count, consumedEvents.Count);
		foreach (var consumedEvent in consumedEvents)
		{
			Assert.IsNotNull(consumedEvent);
			Assert.AreEqual(dtoEvent.Id, consumedEvent!.Id);
			Assert.AreEqual(dtoEvent.DisplayName, consumedEvent.DisplayName);
		}

		// Finalize
		consumer.Stop();
		producer.Stop();

		var channel = ChannelFactory.CreateChannelAsync(cancellationToken).Result;
		channel.ExchangeDelete(consumer.ExchangeName);
		channel.Close();
	}

    [TestMethod]
	public async Task TestPublish()
	{
		// Arrange
		var dtoEvent = new DtoExampleEvent
		{
			Id = 45,
			DisplayName = "Example",
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
		var published = producer.Publish(dtoEvent);

		// Assert
		Assert.IsTrue(published);
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
		var consumer = new GenericConsumer<DtoExampleEvent>(new NullLogger<GenericConsumer<DtoExampleEvent>>(), ChannelFactory, RabbitMqSettings, DtoExampleEventRoutingKeyHelper.CreateConsumerKey(46));

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
	public async Task TestRestartAsync()
	{
		// Arrange
		var dtoEvent = new DtoExampleEvent
		{
			Id = 45,
			DisplayName = "Example",
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

		producer.Stop();

		await producer.StartAsync(cancellationToken);

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