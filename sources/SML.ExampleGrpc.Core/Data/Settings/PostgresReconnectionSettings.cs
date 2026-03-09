namespace SML.ExampleGrpc.Core.Data.Settings;

public class PostgresReconnectionSettings
{
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(30);

    public int NumberOfRetriesBeforeKill { get; set; } = 3;
}
