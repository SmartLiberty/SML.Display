namespace SML.Display.Core.Database;

using Data.Storable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Database context.
/// </summary>
public class DatabaseContext : DbContext
{
    private readonly TimeProvider _timeProvider;

    public DbSet<Example> Examples { get; set; } = null!;

    public DatabaseContext(DbContextOptions<DatabaseContext> options)
        : base(options)
    {
        _timeProvider = this.GetService<TimeProvider>();
    }
        
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseIdentityColumns();
        modelBuilder.ApplyConfiguration(new ExamplesConfiguration());
    }
    
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        OnBeforeSaving();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        OnBeforeSaving();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
    
    private void OnBeforeSaving()
    {
        var utcNow = _timeProvider.GetUtcNow();
        
        foreach (EntityEntry<AuditableEntity> entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                case EntityState.Modified:
                    entry.Entity.LastUpdated = utcNow;
                    break;
            }
        }
        
        //INFO : alternative to AuditableEntity type, use EF Core shadow property
        // foreach (EntityEntry entry in ChangeTracker.Entries().Where(e => e.Properties.Any(p => p.Metadata.Name == "LastUpdated" && p.Metadata.IsShadowProperty())))
        // {
        //     switch (entry.State)
        //     {
        //         case EntityState.Added:
        //         case EntityState.Modified:
        //             entry.Property("LastUpdated").CurrentValue = utcNow;
        //             break;
        //     }
        // }
    }
}
