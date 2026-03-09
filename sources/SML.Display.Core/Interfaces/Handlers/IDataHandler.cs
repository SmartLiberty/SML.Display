namespace SML.Display.Core.Interfaces.Handlers;

/// <summary>
/// Interface for data handler.
/// </summary>
public interface IDataHandler
{
    /// <summary>
    /// Update the display name of an example.
    /// </summary>
    /// <param name="id">Id of the example to update.</param>
    /// <param name="displayName">New displayName of the example.</param>
    Task UpdateExampleDisplayName(long id, string displayName);
}
