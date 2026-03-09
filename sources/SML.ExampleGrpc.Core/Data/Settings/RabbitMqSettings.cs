namespace SML.ExampleGrpc.Core.Data.Settings;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// RabbitMQ settings.
/// </summary>
public class RabbitMqSettings
{
	/// <summary>
	/// The host to connect to.
	/// </summary>
	[Required]
	public string HostName { get; set; } = null!;

	/// <summary>
	/// The port to connect on.
	/// </summary>
	[Range(1, int.MaxValue)]
	public int Port { get; set; }

	/// <summary>
	/// sername to use when authenticating to the server.
	/// </summary>
	[Required]
	public string UserName { get; set; } = null!;

	/// <summary>
	/// Password to use when authenticating to the server.
	/// </summary>
	[Required]
	public string Password { get; set; } = null!;

	/// <summary>
	/// Prefix of all exchange names.
	/// </summary>
	public string ExchangeNamesPrefix { get; set; } = "";

	/// <summary>
	/// Maximum number of attempts to connect.
	/// </summary>
	[Range(1, int.MaxValue)]
	public int MaxAttempts { get; set; }

	/// <summary>
	/// Initial backoff interval between each attempt to connect.
	/// </summary>
	public TimeSpan InitialBackoff { get; set; }

	/// <summary>
	/// Maximum backoff interval between each attempt to connect.
	/// </summary>
	public TimeSpan MaxBackoff { get; set; }

	/// <summary>
	/// Multiplier of backoff interval.
	/// </summary>
	public double BackoffMultiplier { get; set; }
}