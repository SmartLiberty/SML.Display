namespace SML.Display.Core.Database;

public abstract class AuditableEntity
{
    public DateTimeOffset LastUpdated { get; set; }
}
