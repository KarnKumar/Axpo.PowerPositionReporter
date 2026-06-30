using Axpo.PowerPositionReporter.Application.Configurations;
using Axpo.PowerPositionReporter.Domain.Interfaces;
using Axpo.PowerPositionReporter.Domain.Models;
using Microsoft.Extensions.Options;

namespace Axpo.PowerPositionReporter.Application.Services
    {
    /// <summary>
    /// Service class that implements the IPowerPositionService interface to run the Power Position Reporter service.
    /// </summary>
    public class PowerPositionReportService (
        IPowerTradeService powerTradeService,
        IReportWriter csvReportWriter,
        IReportLogger logger,
        IOptions<AppSettings> settings ) : IPowerPositionReportService
        {
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(settings.Value.IntervalMinutes);

        public async Task RunPowerTradePositionReporter ( CancellationToken cancellationToken )
            {
            logger.Info ($"[Power Position Reporter] Started │ interval={_interval} minutes.");

            while ( !cancellationToken.IsCancellationRequested )
                {
                // As per requirement, the first run should be immediate, and subsequent runs should be based on the configured interval.
                await GenerateAndWriteReportAsync (cancellationToken);

                using var timer = new PeriodicTimer(_interval);
                LogNextRun ();

                try
                    {
                    while ( await timer.WaitForNextTickAsync (cancellationToken) )
                        {
                        logger.Info ("[Power Position Reporter] Scheduled tick");

                        await GenerateAndWriteReportAsync (cancellationToken);

                        LogNextRun ();
                        }
                    }
                catch ( OperationCanceledException )
                    {
                    logger.Info ("[Power Position Reporter] Stopped │ cancellation requested");
                    }
                catch ( Exception ex )
                    {
                    logger.Info ($"[Power Position Reporter] Stopped │ due to exception : ¨{ex.Message}");
                    }
                }
            }

        private async Task GenerateAndWriteReportAsync ( CancellationToken cancellationToken )
            {
            var aggregatedPositions = await GetAggregateTradePositionsAsync(cancellationToken);
            await csvReportWriter.WriteAsync (aggregatedPositions, cancellationToken);
            }

        private void LogNextRun ( )
            {
            var nextRunUtc = DateTime.UtcNow.Add(_interval);
            logger.Info ($"[Power Position Reporter]  Next run scheduled at {nextRunUtc:yyyy-MM-dd HH:mm} UTC");
            }

        private async Task<Domain.Models.PowerTrade> GetAggregateTradePositionsAsync ( CancellationToken cancellationToken )
            {
            var aggregatedPositions = new Domain.Models.PowerTrade
                {
                Date = DateTime.UtcNow,
                AggregatedPositions = []
                };

            try
                {
                logger.Info ("[PowerPositionReporter] Trade(s) Extraction started.");

                // Day Ahead
                var dayAheadDate = DateTime.UtcNow.Date.AddDays(1);

                aggregatedPositions = await powerTradeService.GetAggregateTradePositionsAsync (dayAheadDate, cancellationToken);

                logger.Info ("[Power Trade Positions] Trade(s) Extraction completed");
                }
            catch ( OperationCanceledException )
                {
                logger.Info ("[Power Trade Positions] Trade(s) Extraction cancelled");
                throw;
                }
            catch ( Exception ex )
                {
                logger.Error ($"[Power Trade Positions] Trade(s) Extraction FAILED │ reason={ex.Message}");
                }

            return aggregatedPositions;
            }
        }
    }