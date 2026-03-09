namespace SML.ExampleGrpc.Core.Handlers;

using Core.Helpers;
using Core.Interfaces.Handlers;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service process killer.
/// </summary>
public class ProcessKiller : IProcessKiller
{
	private readonly ILogger<ProcessKiller> _logger;

	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="logger">Logger of the service process killer.</param>
	public ProcessKiller(ILogger<ProcessKiller> logger)
	{
		_logger = logger;
		_logger.LogTrace("");
	}

    /// <summary>
    /// Kill the current service process.
    /// </summary>
    /// <param name="reason">Reason of the service process killing.</param>
    public void Kill(string reason)
	{
        EnvironmentHelper.FailFast(_logger, reason);
	}
}
