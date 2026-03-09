namespace SML.Display.Core.Data.Settings;

using Grpc.Net.Client.Configuration;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Settings for gRPC client.
/// </summary>
public class GrpcClientSettings
{
    /// <summary>
    /// URL for the gRPC service.
    /// </summary>
    [Required]
    public string Url { get; set; } = null!;

    /// <summary>
    /// Retry policy.
    /// </summary>
    public RetryPolicy? RetryPolicy { get; set; }
}
