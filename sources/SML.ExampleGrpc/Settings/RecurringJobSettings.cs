namespace SML.ExampleGrpc.Settings;

using Core.Data.Settings;

/// <summary>
/// Settings for recurring jobs.
/// </summary>
internal class RecurringJobSettings
{
	public const string Watchdog = "Watchdog";
	public const string CleanArchives = "CleanArchives";
    public const string LoggOneLine = "LoggOneLine";

    /// <summary>
    /// Delay before the first job.
    /// </summary>
    public TimeSpan JobInitialDelay { get; set; } = TimeSpan.FromSeconds(0);

    /// <summary>
    /// Interval between a successful job and the next one.
    /// </summary>
    public TimeSpan JobInterval { get; set; } = TimeSpan.FromHours(12);

    /// <summary>
    /// Settings of failed job.
    /// </summary>
    public BackoffSettings? FailedJobSettings { get; set; }
}