namespace SML.Display.Core.Events;

using Core.Data.Settings;
using Core.Interfaces.Events;
using Core.Interfaces.Handlers;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

/// <summary>
/// Factory of RabbitMQ channel.
/// </summary>
public class RabbitMqChannelFactory : IRabbitMqChannelFactory
{
    private readonly ILogger<RabbitMqChannelFactory> _logger;

    private readonly IConnectionFactory _connectionFactory;
    private readonly IProcessKiller _processKiller;
    private readonly RabbitMqSettings _settings;

    private readonly object _lockConnection;
    private IConnection? _connection;

	private readonly CancellationTokenSource _cancellationTokenSource;

    private readonly object _reconnectingLocker;
    private bool _isReconnecting;

	private bool _logUnreachableError;
	private bool _logUnexpectedConnectionError;
	private bool _logConnectionClosedError;
	private bool _logUnexpectedChannelError;

	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="logger">Logger.</param>
	/// <param name="connectionFactory">RabbitMQ connection factory.</param>
	/// <param name="processKiller">Process killer.</param>
	/// <param name="settings">RabbitMQ settings.</param>
	public RabbitMqChannelFactory(ILogger<RabbitMqChannelFactory> logger,
        IConnectionFactory connectionFactory,
        IProcessKiller processKiller,
        RabbitMqSettings settings)
    {
        _logger = logger;
        _logger.LogTrace("");

        _connectionFactory = connectionFactory;
		_processKiller = processKiller;
        _settings = settings;

        _lockConnection = new();

		_cancellationTokenSource = new();

        _reconnectingLocker = new();

		_logUnreachableError = true;
		_logUnexpectedConnectionError = true;
		_logConnectionClosedError = true;
		_logUnexpectedChannelError = true;
    }

    /// <summary>
    /// Create a channel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous task result with created channel.</returns>
    public async Task<IModel> CreateChannelAsync(CancellationToken cancellationToken)
    {
        var attempts = _settings.MaxAttempts;
        var backoff = _settings.InitialBackoff;
        while (!cancellationToken.IsCancellationRequested)
        {
            var connection = await GetConnectionAsync(cancellationToken);
            try
            {
                var channel = connection.CreateModel();
                if (!_logConnectionClosedError
                    || !_logUnexpectedChannelError)
                {
                    _logConnectionClosedError = true;
                    _logUnexpectedChannelError = true;
                    _logger.LogInformation("RabbitMQ errors solved");
                }
                _logger.LogTrace("RabbitMQ channel was successfully created");
                return channel;
            }
            catch (AlreadyClosedException)
            {
                if (_logConnectionClosedError)
                {
                    _logConnectionClosedError = false;
                    _logger.LogError("RabbitMQ connection closed");
                }
            }
            catch (Exception e)
            {
                if (_logUnexpectedChannelError)
                {
                    _logUnexpectedChannelError = false;
                    _logger.LogError(e, "Unexpected error occurs");
                }
            }
            if (--attempts <= 0)
            {
                _processKiller.Kill("Maximum attempts have been reached for RabbitMQ channel creation!");
            }
            _logger.LogTrace("{RabbitMqChannelConnectionRemainingAttempts} remaining attempts, following backoff: {RabbitMqChannelConnectionBackoff}", attempts, backoff.ToString(@"hh\:mm\:ss"));
            await Task.Delay(backoff, cancellationToken);
            backoff = Increase(backoff);
        }
        throw new TaskCanceledException("RabbitMQ channel creation is cancelled");
    }

    /// <summary>
    /// Increase the backoff for attempts.
    /// </summary>
    /// <param name="backoff">Previous backoff to increase.</param>
    /// <returns>Increased backoff.</returns>
    public TimeSpan Increase(TimeSpan backoff)
	{
		if (backoff >= _settings.MaxBackoff)
		{
			return _settings.MaxBackoff;
		}
		backoff *= _settings.BackoffMultiplier;
		return backoff > _settings.MaxBackoff
			? _settings.MaxBackoff
			: backoff;
    }

    /// <summary>
    /// Dispose.
    /// </summary>
    public void Dispose()
    {
        lock (_lockConnection)
        {
            if (_connection is { } connection)
            {
                connection.ConnectionShutdown -= Shutdown;
                _cancellationTokenSource.Cancel();
                connection.Close();
            }
            _connection = null;
        }
        _logger.LogInformation("RabbitMQ connection was closed");
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        var attempts = _settings.MaxAttempts;
        var backoff = _settings.InitialBackoff;
		while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_connection is { } existingConnection)
                {
                    return existingConnection;
                }
                IConnection createdConnection;
                lock (_lockConnection)
                {
                    if (_connection is { } recentConnection)
                    {
                        return recentConnection;
                    }
                    createdConnection = _connectionFactory.CreateConnection();
                    _connection = createdConnection;
                }
                createdConnection.ConnectionShutdown += Shutdown;
                _logger.LogInformation("RabbitMQ connection was successfully established");
				_logUnreachableError = true;
				_logUnexpectedConnectionError = true;
				return createdConnection;
            }
            catch (BrokerUnreachableException)
            {
                if (_logUnreachableError)
                {
					_logUnreachableError = false;
                    _logger.LogError("RabbitMQ unreachable");
                }
            }
            catch (Exception e)
            {
                if (_logUnexpectedConnectionError)
                {
					_logUnexpectedConnectionError = false;
                    _logger.LogError(e, "Unexpected error occurs");
                }
            }
            if (--attempts <= 0)
            {
                _processKiller.Kill("Maximum attempts have been reached for RabbitMQ connection!");
            }
            _logger.LogTrace("{RabbitMqConnectionRemainingAttempts} remaining attempts, following backoff: {RabbitMqConnectionBackoff}", attempts, backoff.ToString(@"hh\:mm\:ss"));
            await Task.Delay(backoff, cancellationToken);
            backoff = Increase(backoff);
        }
        throw new TaskCanceledException("RabbitMQ connection is cancelled");
    }

    private void Shutdown(object? sender, ShutdownEventArgs e)
    {
        lock (_reconnectingLocker)
        {
            if (_isReconnecting)
            {
                return;
            }
            _isReconnecting = true;
        }
        _logger.LogError("RabbitMQ connection shutdown");
        var cancellationToken = _cancellationTokenSource.Token;
        var attempts = _settings.MaxAttempts;
        var backoff = _settings.InitialBackoff;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (attempts <= 0)
            {
                _processKiller.Kill("Maximum attempts have been reached for RabbitMQ reconnection!");
            }
            _logger.LogTrace("{RabbitMqReconnectionRemainingAttempts} remaining attempts, following backoff: {RabbitMqReconnectionBackoff}", attempts, backoff.ToString(@"hh\:mm\:ss"));
            Task.Delay(backoff, cancellationToken).Wait(cancellationToken);
            if (_connection?.IsOpen ?? false)
            {
                _logger.LogInformation("RabbitMQ was successfully reconnected");
                _isReconnecting = false;
                return;
            }
            attempts--;
            backoff = Increase(backoff);
        }
        throw new TaskCanceledException("RabbitMQ reconnection is cancelled");
    }
}
