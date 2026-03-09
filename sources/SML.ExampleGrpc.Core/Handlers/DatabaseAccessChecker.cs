namespace SML.ExampleGrpc.Core.Handlers;

using Core.Database;
using Core.Interfaces.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class DatabaseAccessChecker : IDatabaseAccessChecker
{
    private readonly ILogger<DatabaseAccessChecker> _logger;
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly IWaitingHandler _waitingHandler;
    private bool DatabaseAccessible { get; set; }

    public DatabaseAccessChecker(ILogger<DatabaseAccessChecker> logger,
        IDbContextFactory<DatabaseContext> dbContextFactory,
        IWaitingHandler waitingHandler)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _waitingHandler = waitingHandler;
    }

    public async Task<bool> IsDatabaseAccessible(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogTrace("Begin");

            await using var databaseContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            DatabaseAccessible = await databaseContext.Database.CanConnectAsync(cancellationToken);

            if (DatabaseAccessible)
            {
                _logger.LogInformation("Database is available for connections!");
                _waitingHandler.Set();
            }
            else
            {
                _logger.LogWarning("Database is NOT available for connections!");
            }
        }
        //old way to check for connection issues
        catch (Exception ex)
            when (ex.GetType() == typeof(InvalidOperationException)
                  && ex.Source != null
                  && ex.Source.Contains("Npgsql"))
        {
            _logger.LogError(ex, "Failed with InvalidOperationException Exception for Npgsql!");

            DatabaseAccessible = false;
        }

        return DatabaseAccessible;
    }

    public async Task<bool> WaitDatabaseAccessAsync(CancellationToken cancellationToken)
    {
        return await _waitingHandler.WaitAsync(cancellationToken);
    }
}
