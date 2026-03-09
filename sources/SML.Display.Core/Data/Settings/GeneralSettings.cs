namespace SML.Display.Core.Data.Settings;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// General settings.
/// </summary>
public class GeneralSettings
{
    /// <summary>
    /// Name of the current service.
    /// </summary>
    [Required]
    public string ServiceName { get; set; } = null!;

    /// <summary>
    /// Description of the current service.
    /// </summary>
    [Required]
    public string ServiceDescription { get; set; } = null!;

    /// <summary>
    /// URL of the health check.
    /// </summary>
    [Required]
    public string HealthCheckUrl { get; set; } = null!;
}
