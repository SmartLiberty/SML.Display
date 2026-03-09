namespace SML.Display.Core.Data.Settings;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Settings for backoff.
/// </summary>
public class BackoffSettings
{
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
