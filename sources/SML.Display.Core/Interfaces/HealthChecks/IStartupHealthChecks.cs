namespace SML.Display.Core.Interfaces.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

public interface IStartupHealthChecks : IHealthCheck
{
    void ServiceStarted();
}
