namespace SML.ExampleGrpc.Core.Watchdog.Dtos;

using MessagePack;
using System.Collections.Generic;

/// <summary>
/// Stores information necessary for a Service Registration
/// </summary>
[Obsolete("To be deleted after watchdog refactoring.")]
[MessagePackObject(true)]
public class DtoServiceRegistration
{
    /// <summary>
    /// Service Name to be displayed
    /// </summary>
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Service EXE name to be verified
    /// </summary>
    public string Exe { get; set; } = "";

    /// <summary>
    /// List of event types to be registered wth Service
    /// </summary>
    public List<DtoEventRegistration> Events { get; set; } = new List<DtoEventRegistration>();
}