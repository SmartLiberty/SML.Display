namespace SML.ExampleGrpc.Tests.Integration.Events;

using Core.Data.Settings;
using Core.Events;
using Core.Interfaces.Events;
using MessagePack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;
using SML.Shared.RabbitMq.Rpc;
using System.Threading.Tasks;

[TestClass]
public class RpcRabbitMqTest
{
    private static RabbitMqSettings RabbitMqSettings = null!;
	private static IRabbitMqChannelFactory ChannelFactory = null!;

	[ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        var config = Config.InitConfiguration();
        RabbitMqSettings = config.GetSection(nameof(RabbitMqSettings)).Get<RabbitMqSettings>();
		ChannelFactory = new RabbitMqChannelFactory(new NullLogger<RabbitMqChannelFactory>(), new ConnectionFactory
		{
            HostName = RabbitMqSettings.HostName,
            Port = RabbitMqSettings.Port,
            UserName = RabbitMqSettings.UserName,
            Password = RabbitMqSettings.Password
        }, new TestFailer(), RabbitMqSettings);
	}

    [TestMethod]
    public async Task TestNotSupportedStartAsync()
    {
        IGenericRpcRabbitMqServer<ExampleRequest, ExampleResponse> server = new GenericRpcRabbitMqServer<ExampleRequest, ExampleResponse>(new NullLogger<GenericRpcRabbitMqServer<ExampleRequest, ExampleResponse>>(), ChannelFactory, RabbitMqSettings);
        
		var cancellationTokenSource = new CancellationTokenSource();
		var cancellationToken = cancellationTokenSource.Token;

        try
        {
            await server.StartAsync(cancellationToken);
            Assert.Fail();
        }
        catch (NotSupportedException e)
        {
            Assert.AreEqual("Use Start function with function to consume and reply", e.Message);
		}
    }

    [TestMethod]
    public void TestClientServer()
    {
        IGenericRpcRabbitMqServer<ExampleRequest, ExampleResponse> server = new GenericRpcRabbitMqServer<ExampleRequest, ExampleResponse>(new NullLogger<GenericRpcRabbitMqServer<ExampleRequest, ExampleResponse>>(), ChannelFactory, RabbitMqSettings);
        IGenericRpcRabbitMqClient<ExampleRequest, ExampleResponse> client = new GenericRpcRabbitMqClient<ExampleRequest, ExampleResponse>(new NullLogger<GenericRpcRabbitMqClient<ExampleRequest, ExampleResponse>>(), ChannelFactory, RabbitMqSettings);
        
		var cancellationTokenSource = new CancellationTokenSource();
		var cancellationToken = cancellationTokenSource.Token;

		server.StartAsync(ExampleRequestHandler, cancellationToken);
        client.StartAsync(cancellationToken);

        var result = client.Publish(new ExampleRequest(12));
        Assert.IsNotNull(result);
        Assert.AreEqual(12, result?.Id);
        Assert.AreEqual("example", result?.Displayname);
        server.Stop();
    }

    [TestMethod]
    public void TestRpcClientTimeout()
    {
        int timeout = 1_000;
        IGenericRpcRabbitMqClient<ExampleRequest, ExampleResponse> client = new GenericRpcRabbitMqClient<ExampleRequest, ExampleResponse>(new NullLogger<GenericRpcRabbitMqClient<ExampleRequest, ExampleResponse>>(), ChannelFactory, RabbitMqSettings, timeout);
		
        var cancellationTokenSource = new CancellationTokenSource();
		var cancellationToken = cancellationTokenSource.Token;

		client.StartAsync(cancellationToken);

        Assert.Throws<RabbitMqRpcException>(() => client.Publish(new ExampleRequest(12)), $"No response after {timeout} milliseconds");

    }
    private ExampleResponse ExampleRequestHandler(ExampleRequest request)
    {
        return new(request.Id, "example");
    }

    [MessagePackObject]
    public record ExampleRequest([property: Key(0)] long Id);

    [MessagePackObject]
    public record ExampleResponse([property: Key(0)] long Id, [property: Key(1)] string Displayname);
}
