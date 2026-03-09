namespace SML.ExampleGrpc.Core.Interfaces.Handlers;

public interface IDatabaseAccessChecker
{
    Task<bool> IsDatabaseAccessible(CancellationToken cancellationToken);

    Task<bool> WaitDatabaseAccessAsync(CancellationToken cancellationToken);
}
