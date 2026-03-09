namespace SML.Display.Core.Data.Storable;

using Core.Database;

/// <summary>
/// Example.
/// </summary>
public class Example : AuditableEntity
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Display name.
    /// </summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Flag specifying if the entity is archived or not.
    /// </summary>
    public bool Archived { get; set; }
        
    /// <summary>
    /// Commentary.
    /// </summary>
    public string? Commentary { get; set; }
}
