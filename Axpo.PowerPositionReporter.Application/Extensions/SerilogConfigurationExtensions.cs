using Axpo.PowerPositionReporter.Application.Configurations;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Axpo.PowerPositionReporter.Application.Extensions
    {
    public static class SerilogConfigurationExtensions
        {
        public static LoggerConfiguration AddFileLogging (
            this LoggerConfiguration loggerConfig,
            IConfiguration configuration,
            IServiceProvider services )
            {
            var ls = configuration
    .GetSection($"{AppSettings.SectionName}:Logging")
    .Get<LoggingSettings>() ?? new LoggingSettings();

            Directory.CreateDirectory (ls.LogDirectory);

            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmm");
            var logPath = Path.Combine(ls.LogDirectory, $"PowerTradePositionReport_{timestamp}.log");
            return loggerConfig
                .ReadFrom.Configuration (configuration)
                .ReadFrom.Services (services)
                .Enrich.FromLogContext ()
                .Enrich.WithProperty ("Application", "Axpo.PowerTradePosition")
                .WriteTo.File (
                    path: logPath,
                    rollingInterval: RollingInterval.Infinite,
                    retainedFileCountLimit: ls.RetainedFileDays == 0 ? null : ls.RetainedFileDays,
                    outputTemplate: ls.OutputTemplate,
                    shared: true)
               .WriteTo.Console (theme: AnsiConsoleTheme.None);
            }
        }
    }
