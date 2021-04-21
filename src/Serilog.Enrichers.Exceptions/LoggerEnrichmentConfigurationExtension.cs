using Serilog.Configuration;

namespace Serilog.Enrichers.Exceptions
{
    public static class LoggerEnrichmentConfigurationExtension
    {
        public static LoggerEnrichmentConfiguration WithFriendlyException(this LoggerEnrichmentConfiguration enrich)
        {
            enrich.With(new ExceptionEnricher());
            return enrich;
        }
    }
}