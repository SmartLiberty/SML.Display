namespace SML.ExampleGrpc.Core.Events;

using Core.Data.Settings;
using Core.Interfaces.Events;
using RabbitMQ.Client;

/// <summary>
/// Base for RabbitMQ consumer and producer.
/// </summary>
public abstract class RabbitMqBase : IRabbitMqBase
{
	/// <summary>
	/// Exchange name.
	/// </summary>
	public string ExchangeName { get; }

	/// <summary>
	/// RabbitMQ channel factory.
	/// </summary>
	protected IRabbitMqChannelFactory ChannelFactory { get; }

	/// <summary>
	/// RabbitMQ settings.
	/// </summary>
	protected RabbitMqSettings Settings { get; }

	/// <summary>
	/// Channel.
	/// </summary>
	protected IModel? Channel { get; private set; }

	private readonly object _startLock;
	private bool _isStarted;

	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="channelFactory">RabbitMQ channel factory.</param>
	/// <param name="settings">RabbitMQ settings.</param>
	/// <param name="exchangeName">RabbitMQ exchange name.</param>
	protected RabbitMqBase(IRabbitMqChannelFactory channelFactory,
		RabbitMqSettings settings,
		string exchangeName)
	{
		ChannelFactory = channelFactory;
		Settings = settings;
		_startLock = new object();
		ExchangeName = $"{settings.ExchangeNamesPrefix}{exchangeName}";
	}

	/// <summary>
	/// Start connection if necessary, create channel and call "Initialize" method.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Asynchronous task result.</returns>
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		lock (_startLock)
		{
			if (_isStarted)
			{
				return;
			}
			_isStarted = true;
		}

		Channel = await ChannelFactory.CreateChannelAsync(cancellationToken);

		Initialize(Channel);
	}

	/// <summary>
	/// Call "End" method, close channel, then stop connection if last one to stop.
	/// </summary>
	public void Stop()
	{
		lock (_startLock)
		{
			if (!_isStarted)
			{
				return;
			}
			_isStarted = false;
		}

		End();

		Channel?.Close();
		Channel = null;
    }

	/// <summary>
	/// Initialize RabbitMQ communication.
	/// </summary>
	protected abstract void Initialize(IModel channel);

	/// <summary>
	/// End RabbitMQ communication.
	/// </summary>
	protected abstract void End();
}