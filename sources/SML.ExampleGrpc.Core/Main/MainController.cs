namespace SML.ExampleGrpc.Core.Main;

using Core.Interfaces.Events;
using Core.Interfaces.Handlers;
using Core.Interfaces.Main;
using Example.Shared.Dtos;
using Grpc.Core;
using Microsoft.Extensions.Logging;

/// <summary>
/// Main controller.
/// </summary>
public class MainController : IMainController
{
    private readonly ILogger<MainController> _logger;
    private readonly IGenericConsumer<DtoExampleEvent> _eventsConsumer;
    private readonly IGenericProducer<DtoExampleEvent> _eventsProducer;
    private readonly IDataHandler _dataHandler;

	private readonly IProcessKiller _processKiller;

	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="logger">Logger.</param>
	/// <param name="eventsConsumer">Events consumer.</param>
	/// <param name="eventsProducer">Events producer.</param>
	/// <param name="dataHandler">Data handler.</param>
	/// <param name="processKiller">Process killer.</param>
	public MainController(ILogger<MainController> logger, 
        IGenericConsumer<DtoExampleEvent> eventsConsumer,
        IGenericProducer<DtoExampleEvent> eventsProducer,
        IDataHandler dataHandler,
		IProcessKiller processKiller)
    {
        _logger = logger;
        _logger.LogTrace("Begin");

        _eventsConsumer = eventsConsumer;
        _eventsProducer = eventsProducer;

        _dataHandler = dataHandler;

		_processKiller = processKiller;
	}

	/// <summary>
	/// Start the controller.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Begin");
        
        _eventsConsumer.ConsumedEvent += ReceiveEvent;

        await _eventsProducer.StartAsync(cancellationToken);

        try
        {
            // TODO var response = _grpcClient.ReadInitialData(new Google.Protobuf.WellKnownTypes.Empty(), cancellationToken: cancellationToken);
        }
        catch (RpcException)
        {
            _processKiller.Kill("Maximum attempts have been reached for gRPC connection!");
        }
        catch (Exception e)
        {
            _processKiller.Kill(e.Message);
        }

        await _eventsConsumer.StartAsync(cancellationToken);

        _logger.LogInformation("End");
    }

    /// <summary>
    /// Stop the controller.
    /// </summary>
    public void Stop()
    {
        _logger.LogInformation("Begin");

        _eventsConsumer.Stop();
        _eventsProducer.Stop();

        _eventsConsumer.ConsumedEvent -= ReceiveEvent;
	}

	private void ReceiveEvent(DtoExampleEvent example)
    {
        _logger.LogDebug("{ExampleDisplayName}", example.DisplayName);

        _dataHandler.UpdateExampleDisplayName(example.Id, example.DisplayName);
    }
}