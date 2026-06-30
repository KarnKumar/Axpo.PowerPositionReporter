using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

using Axpo.PowerPositionReporter.Application.Configurations;
using Axpo.PowerPositionReporter.Application.Services;
using Axpo.PowerPositionReporter.Domain.Interfaces;
using Axpo.PowerPositionReporter.Application.Extensions;
using Axpo.PowerPositionReporter.Worker;
using Axpo;


var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
                optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddCommandLine(args, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                ["--csv-report-path"] = "PowerPositionReporter:CsvReportPath",
                ["-csv"]            = "PowerPositionReporter:CsvReportPath",
                ["--interval"]    = "PowerPositionReporter:IntervalMinutes",
                ["-i"]            = "PowerPositionReporter:IntervalMinutes",
                });
    })
  .UseSerilog((context, services, loggerConfig) =>
  {
      loggerConfig.AddFileLogging(context.Configuration, services);
  })
    .ConfigureServices((context, services) =>
    {
        services.Configure<AppSettings>(
            context.Configuration.GetSection(AppSettings.SectionName));

        services.AddPowerTradeResilience(context.Configuration);

        services.AddSingleton<IReportLogger, SerilogReportLogger>();

        services.AddSingleton<IPowerPositionReportService, PowerPositionReportService>();
        services.AddSingleton<IPowerTradeService, PowerTradeService>();

        services.AddSingleton<IPowerService, PowerService>();
        services.AddSingleton<IReportWriter, CsvReportWriter>();
        services.AddHostedService<PowerPositionReportWorker>();
    })
    .UseConsoleLifetime(options =>
    {
        options.SuppressStatusMessages = true;
    })
    .Build();

// Log startup immediately after host is built
var logger = host.Services.GetRequiredService<IReportLogger>();
logger.Info ($"[STARTUP] Power Position Reporter Application started │ pid={Environment.ProcessId} │ utc={DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC");

try
    {
    await host.RunAsync ();
    logger.Info ("[STARTUP] Power Position Reporter Application stopped cleanly");
    return 0;
    }
catch ( Exception ex )
    {
    logger.Fatal ("[STARTUP] Power Position Reporter Application Terminated unexpectedly", ex);
    return 1;
    }
