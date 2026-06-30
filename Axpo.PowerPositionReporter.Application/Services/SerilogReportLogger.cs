using Axpo.PowerPositionReporter.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Axpo.PowerPositionReporter.Application.Services
    {
    public sealed class SerilogReportLogger ( ILogger<SerilogReportLogger> logger ) : IReportLogger
        {
        private readonly ILogger<SerilogReportLogger> _logger = logger;

        public void Debug ( string message ) => _logger.LogDebug (message);
        public void Info ( string message ) => _logger.LogInformation (message);
        public void Warning ( string message ) => _logger.LogWarning (message);
        public void Error ( string message, Exception? ex = null ) => _logger.LogError (ex, message: message);
        public void Fatal ( string message, Exception? ex = null ) => _logger.LogCritical (ex, message: message);
        
        }
    }
