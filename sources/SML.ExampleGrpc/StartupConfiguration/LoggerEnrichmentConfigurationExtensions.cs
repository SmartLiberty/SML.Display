using Serilog;
using Serilog.Configuration;
using SML.ExampleGrpc.Enrichers;

namespace SML.ExampleGrpc.StartupConfiguration;

/// <summary>
/// Serilog custom enrichers configuration.
/// </summary>
public static class LoggerEnrichmentConfigurationExtensions
{
    extension(LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        public LoggerConfiguration WithUniqueDate()
            => enrichmentConfiguration.With(new UniqueDateEnrichers());

        public LoggerConfiguration WithMethodName(string serviceName)
            => enrichmentConfiguration.With(new MethodNameEnrichers(serviceName));

        public LoggerConfiguration WithCustomMachineName()
            => enrichmentConfiguration.With(new CustomMachineNameEnrichers());

        public LoggerConfiguration WithCustomLevel()
            => enrichmentConfiguration.With(new CustomLevelEnricher());
    }
}