namespace SML.ExampleGrpc.Core.Data.Settings;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Life time settings.
/// </summary>
public class LifeTimeSettings
{
    /// <summary>
    /// Number of days elapsed before archiving an example.
    /// </summary>
    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "The field {0} must be greater than {1}.")]
    public int ArchivedExamplesDays { get; set; } = 0;

    /// <summary>
    /// Time elapsed before archiving an example.
    /// </summary>
    public TimeSpan ArchivedExamplesSpan => new(ArchivedExamplesDays, 0, 0, 0);
}