namespace SML.Display.Core.Interfaces.Services;

public interface IDatabaseAccessCheckerService
{
    Task CheckDatabaseAccessibleAsync(CancellationToken cancellationToken);
}
