using Axpo.PowerPositionReporter.Domain.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Axpo.PowerPositionReporter.Worker
    {

    /// <summary>
    /// Worker class that runs the Power Position Reporter service in the background.
    /// </summary>
    /// <param name="powerPositionService"></param>
    public class PowerPositionReportWorker ( IPowerPositionReportService powerPositionService ) : BackgroundService
        {
        protected override Task ExecuteAsync ( CancellationToken stoppingToken )
            {

            return powerPositionService.RunPowerTradePositionReporter (stoppingToken);
            }
        }
    }
