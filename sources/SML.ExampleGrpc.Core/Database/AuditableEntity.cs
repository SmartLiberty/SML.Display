namespace SML.ExampleGrpc.Core.Database;

public abstract class AuditableEntity
{
    public DateTimeOffset LastUpdated { get; set; }
}