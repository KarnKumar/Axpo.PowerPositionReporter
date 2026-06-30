using Axpo.PowerPositionReporter.Application.Configurations;
using Axpo.PowerPositionReporter.Domain.Exceptions;
using Axpo.PowerPositionReporter.Domain.Interfaces;

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

        /// <summary>
        /// Method to run the Power Position Reporter service, which generates and writes reports at a configured interval until cancellation is requested.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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
                    logger.Error (
                        "[Power Position Reporter] Scheduling loop failed unexpectedly │ restarting timer", ex);
                    }
                }
            }

        private int _consecutiveFailures;
        private const int ConsecutiveFailureAlertThreshold = 3;

        /// <summary>
        /// Generates and writes the report by retrieving aggregated trade positions and writing them to a CSV file.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task GenerateAndWriteReportAsync ( CancellationToken cancellationToken )
            {
            try
                {
                var aggregatedPositions = await GetAggregateTradePositionsAsync(cancellationToken);
                await csvReportWriter.WriteAsync (aggregatedPositions, cancellationToken);

                _consecutiveFailures = 0;
                }
            catch ( OperationCanceledException )
                {
                logger.Info ("[Power Trade Positions] Trade(s) Extraction cancelled");
                throw;
                }
            catch ( PowerServiceUnavailableException ex )
                {
                // Expected, retryable failure: the resilience pipeline already exhausted its
                // retries for this run. Log as a warning and let the next scheduled tick try again.
                _consecutiveFailures++;
                logger.Warning (
                    $"[Power Trade Positions] Trade(s) Extraction FAILED (consecutive={_consecutiveFailures}) │ power service unavailable │ reason={ex.Message}");
                EscalateIfRepeatedFailure ();
                }
            catch ( ReportWriteException ex )
                {
                // Expected, retryable failure: report could not be written this run (disk/permissions).
                _consecutiveFailures++;
                logger.Error ($"[Power Trade Positions] Report write FAILED (consecutive={_consecutiveFailures})", ex);
                EscalateIfRepeatedFailure ();
                }
            catch ( Exception ex )
                {
                // Unexpected, unclassified failure. Logged at Fatal so it is clearly distinguishable
                // from the expected/retryable failures above (e.g. config errors, bugs, OOM).
                _consecutiveFailures++;
                logger.Fatal (
                    $"[Power Trade Positions] Trade(s) Extraction FAILED unexpectedly (consecutive={_consecutiveFailures})", ex);
                EscalateIfRepeatedFailure ();
                }
            }

        /// <summary>
        /// Raises visibility once failures repeat across several consecutive scheduled runs,
        /// so a systemic problem isn't silently retried forever without anyone noticing.
        /// </summary>
        private void EscalateIfRepeatedFailure ( )
            {
            if ( _consecutiveFailures >= ConsecutiveFailureAlertThreshold )
                {
                logger.Fatal (
                    $"[Power Position Reporter] {_consecutiveFailures} consecutive report runs have failed │ this likely indicates a systemic problem (not a transient blip)");
                }
            }

        private void LogNextRun ( )
            {
            var nextRunUtc = DateTime.UtcNow.Add(_interval);
            logger.Info ($"[Power Position Reporter]  Next run scheduled at {nextRunUtc:yyyy-MM-dd HH:mm} UTC");
            }

        /// <summary>
        /// Retrieves the aggregated trade positions for the next day from the power trade service.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Domain.Models.PowerTrade> GetAggregateTradePositionsAsync ( CancellationToken cancellationToken )
            {
            logger.Info ("[Power Position Reporter] Trade(s) Extraction started.");

            var dayAheadDate = DateTime.UtcNow.Date.AddDays(1);
            var aggregatedPositions = await powerTradeService.GetAggregateTradePositionsAsync (dayAheadDate, cancellationToken);

            logger.Info ("[Power Trade Positions] Trade(s) Extraction completed");

            return aggregatedPositions;
            }
        }
    }