namespace SML.Display.Core.Data.Settings;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Settings for breaker.
/// </summary>
public class BreakerSettings
{
    /// <summary>
    /// Number of failures before breaking.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int FailuresBeforeBreaking { get; set; }

    /// <summary>
    /// Duration of the break.
    /// </summary>
    public TimeSpan BreakDuration { get; set; }
}
