namespace SML.ExampleGrpc.Core.Interfaces.Services;

public interface IDatabaseAccessCheckerService
{
    Task CheckDatabaseAccessibleAsync(CancellationToken cancellationToken);
}