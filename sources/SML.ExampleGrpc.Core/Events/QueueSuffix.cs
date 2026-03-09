namespace SML.ExampleGrpc.Core.Events;

/// <summary>
/// Simple wrapper to solve the constructor issue in this specific case of
/// migrating from .NET6 to .NET8:
/// <see href="https://learn.microsoft.com/en-gb/dotnet/core/compatibility/extensions/8.0/activatorutilities-createinstance-behavior#new-behavior">HERE</see>
/// </summary>
public class QueueSuffix(string suffix)
{
    private readonly string _suffix = suffix;

    public string AddSuffix(string queue)
        => $"{queue}-{_suffix}";
}
