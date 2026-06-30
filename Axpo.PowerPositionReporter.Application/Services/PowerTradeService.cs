using Axpo.PowerPositionReporter.Application.Extensions;
using Axpo.PowerPositionReporter.Domain.Interfaces;

using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;

namespace Axpo.PowerPositionReporter.Application.Services
    {
    /// <summary>
    /// Implementation of the IPowerTradeService interface that handles power trade operations.
    /// </summary>
    public class PowerTradeService (
        IPowerService powerService,
        ResiliencePipelineProvider<string> pipelineProvider,
        ILogger<PowerTradeService> logger ) : IPowerTradeService
        {
        private readonly ResiliencePipeline<IEnumerable<PowerTrade>> _pipeline =
            pipelineProvider.GetPipeline<IEnumerable<PowerTrade>>(
                ResilienceServiceExtensions.PowerTradeRetryPipeline);

        /// <summary>
        /// Gets the aggregated trade positions for a specific date.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Domain.Models.PowerTrade> GetAggregateTradePositionsAsync ( DateTime date, CancellationToken cancellationToken = default )
            {
            logger.LogDebug ("[POWER-SVC] GetTradesAsync │ date={Date:yyyy-MM-dd}", date);

            try
                {
                // Execute the GetTradesAsync method with resilience and retry policies.
                var trades = await _pipeline.ExecuteAsync(
                    async ct => await powerService.GetTradesAsync(date),
                    cancellationToken);

                if ( trades == null || !trades.Any () )
                    {
                    logger.LogWarning ("[POWER-SVC] GetTradesAsync │ no trades returned │ date={Date:yyyy-MM-dd}", date);

                    return new Domain.Models.PowerTrade
                        {
                        Date = date,
                        AggregatedPositions = []
                        };
                    }

                // Sum raw volumes per period, then round once on the final total (not per trade).
                var rawPositions = SumTradePositions(date, trades);
                var roundedPositions = rawPositions.ToDictionary (p => p.Key, p => Math.Round (p.Value, 0));

                return new Domain.Models.PowerTrade
                    {
                    Date = date,
                    AggregatedPositions = roundedPositions
                    };
                }
            catch ( Exception ex )
                {
                logger.LogError (ex,
                    "[POWER-SVC] GetTradesAsync FAILED │ date={Date:yyyy-MM-dd} │ type={ExceptionType} │ reason={ErrorMessage}",
                    date, ex.GetType ().Name, ex.Message);
                throw;
                }
            }

        /// <summary>
        /// Aggregates the trade positions by period for a given date and collection of trades.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="trades"></param>
        /// <returns></returns>
        private Dictionary<int, double> SumTradePositions ( DateTime date, IEnumerable<PowerTrade> trades )
            {
            var rawPositions = new Dictionary<int, double>();
            var tradeCount = 0;

            foreach ( var trade in trades )
                {
                tradeCount++;

                foreach ( var period in trade.Periods )
                    {
                    rawPositions[period.Period] = rawPositions.TryGetValue (period.Period, out var existing)
                        ? existing + period.Volume
                        : period.Volume;
                    }
                }

            logger.LogDebug (
                "[POWER-SVC] GetTradesAsync │ date={Date:yyyy-MM-dd} │ tradeCount={TradeCount}",
                date, tradeCount);

            return rawPositions;
            }
        }
    }