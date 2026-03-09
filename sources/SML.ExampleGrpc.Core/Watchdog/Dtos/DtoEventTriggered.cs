namespace SML.ExampleGrpc.Core.Watchdog.Dtos;

using MessagePack;
using System;

/// <summary>
/// Stores all values that represent a Message Entry in RabbitMQ for an Service Event
/// </summary>
[Obsolete("To be deleted after watchdog refactoring.")]
[MessagePackObject(true)]
public class DtoEventTriggered
{
    /// <summary>
    /// Service Event Identifier
    /// </summary>
    public long Id { get; set; } = -1;

    /// <summary>
    /// DateTime at which Event has run
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.MinValue;
}