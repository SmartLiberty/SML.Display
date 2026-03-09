namespace SML.ExampleGrpc.Core.Data.Settings;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Settings of watchdog client.
/// </summary>
public class WatchdogClientSettings
{
	/// <summary>
	/// Name of the exe file of service which is being watched.
	/// </summary>
	[Required]
	public string ServiceExe { get; set; } = null!;

	/// <summary>
	/// Period used by registered events.
	/// </summary>
	[Range(1, int.MaxValue)]
	public int Period { get; set; } = 60;

	/// <summary>
	/// Grace period used by registered Events.
	/// </summary>
	[Range(1, int.MaxValue)]
	public int GracePeriod { get; set; } = 120;
}