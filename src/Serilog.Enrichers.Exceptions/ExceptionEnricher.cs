using Serilog.Core;
using Serilog.Events;

namespace Serilog.Enrichers.Exceptions
{
    public class ExceptionEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent.Exception == null)
                return;

            var logEventProperty = propertyFactory.CreateProperty("FriendlyException",
                logEvent.Exception.ToFriendlyMessage());
            logEvent.AddPropertyIfAbsent(logEventProperty);
        }
    }
}