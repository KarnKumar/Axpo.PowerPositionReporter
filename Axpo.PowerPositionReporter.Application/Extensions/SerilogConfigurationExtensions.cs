using Axpo.PowerPositionReporter.Application.Configurations;

using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Axpo.PowerPositionReporter.Application.Extensions
    {

    /// <summary>
    /// serilog configuration extensions for adding file logging to the logger configuration.
    /// </summary>
    public static class SerilogConfigurationExtensions
        {
        public static LoggerConfiguration AddFileLogging (
            this LoggerConfiguration loggerConfig,
            IConfiguration configuration,
            IServiceProvider services )
            {
            var loggingSettings = configuration
                .GetSection($"{AppSettings.SectionName}:Logging")
                .Get<LoggingSettings>() ?? new LoggingSettings();

            Directory.CreateDirectory (loggingSettings.LogDirectory);

            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmm");
            var logPath = Path.Combine(loggingSettings.LogDirectory, $"PowerTradePositionReport_{timestamp}.log");

            return loggerConfig
                .ReadFrom.Configuration (configuration)
                .ReadFrom.Services (services)
                .Enrich.FromLogContext ()
                .Enrich.WithProperty ("Application", "Axpo.PowerTradePosition")
                .WriteTo.File (
                    path: logPath,
                    rollingInterval: RollingInterval.Infinite,
                    retainedFileCountLimit: loggingSettings.RetainedFileDays == 0 ? null : loggingSettings.RetainedFileDays,
                    outputTemplate: loggingSettings.OutputTemplate,
                    shared: true)
                .WriteTo.Console (theme: AnsiConsoleTheme.None);
            }
        }
    }