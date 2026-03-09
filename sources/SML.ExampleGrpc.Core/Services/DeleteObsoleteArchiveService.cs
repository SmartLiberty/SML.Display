namespace SML.ExampleGrpc.Core.Services;

using Core.Data.Settings;
using Core.Database;
using Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class DeleteObsoleteArchiveService : IDeleteObsoleteArchiveService
{
    private readonly ILogger<DeleteObsoleteArchiveService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly LifeTimeSettings _lifeTimeSettings;
    private readonly DatabaseContext _dbContext;

    public DeleteObsoleteArchiveService(DatabaseContext dbContext, 
        TimeProvider timeProvider, 
        IOptions<LifeTimeSettings> lifeTimeSettings, 
        ILogger<DeleteObsoleteArchiveService> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _lifeTimeSettings = lifeTimeSettings.Value;
        _logger = logger;
    }
    
    public async Task DeleteObsoleteArchive()
    {
        var examples = _dbContext.Examples
            .Where(x => x.Archived && x.LastUpdated < _timeProvider.GetUtcNow() - _lifeTimeSettings.ArchivedExamplesSpan)
            .ToList();
        
        _logger.LogDebug("{DeleteExamplesCount} examples to delete have been found", examples.Count);
        
        if (examples.Count > 0)
        {
            _dbContext.Examples.RemoveRange(examples);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("{DeletedExamplesCount} archived examples have been deleted", examples.Count);
        }
    }
}