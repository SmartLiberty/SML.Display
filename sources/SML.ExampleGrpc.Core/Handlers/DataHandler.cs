namespace SML.ExampleGrpc.Core.Handlers;

using Database;
using Interfaces.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Data handler.
/// </summary>
public class DataHandler : IDataHandler
{
    private readonly ILogger<DataHandler> _logger;

    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="dbContextFactory">Db context factory.</param>
    public DataHandler(ILogger<DataHandler> logger,
         IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _logger.LogTrace("Begin");
    }

    /// <summary>
    /// Update the display name of an example.
    /// </summary>
    /// <param name="id">Id of the example to update.</param>
    /// <param name="displayName">New displayName of the example.</param>
    public async Task UpdateExampleDisplayName(long id, string displayName)
    {
        _logger.LogDebug("Example[{ExampleId}] {ExampleDisplayName}", id, displayName);

        using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var example = await dbContext.Examples.FindAsync(id);

        if (example == null)
        {
            _logger.LogWarning("Example[{ExampleId}] '{ExampleDisplayName}' does not exist!", id, displayName);
            return;
        }

        if (example.Archived)
        {
            _logger.LogWarning("Example[{ExampleId}] '{ExampleDisplayName}' is archived!", example.Id, example.DisplayName);
            return;
        }

        if (await dbContext.Examples.Where(x => x.Id != id && x.DisplayName.Equals(displayName)).AnyAsync())
        {
            _logger.LogWarning("n example with the name '{ExampleDisplayName}' is already exist!", example.DisplayName);
            return;
        }

        example.DisplayName = displayName;

        if ((await dbContext.SaveChangesAsync()) == 0)
        {
            _logger.LogInformation("Example[{ExampleId}] does not need to be updated!", example.Id);
        }

        await dbContext.SaveChangesAsync();
    }
}