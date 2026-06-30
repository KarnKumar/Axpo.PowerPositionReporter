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
    public class PowerTradeService ( IPowerService PowerService, ResiliencePipelineProvider<string> pipelineProvider, ILogger<PowerTradeService> logger ) : IPowerTradeService
        {

        private readonly ResiliencePipeline<IEnumerable<PowerTrade>> _pipeline =
            pipelineProvider.GetPipeline<IEnumerable<PowerTrade>>(
                ResilienceServiceExtensions.PowerTradeRetryPipeline);

        public async Task< Domain.Models.PowerTrade> GetAggregateTradePositionsAsync ( DateTime date , CancellationToken cancellationToken = default )
            {

            logger.LogDebug ("[POWER-SVC] GetTradesAsync │ date={Date:yyyy-MM-dd}", date);

            try
                {

                /// Execute the GetTradesAsync method with resilience and retry policies.
                var trades = await _pipeline.ExecuteAsync(
                    async ct => await PowerService.GetTradesAsync(date),
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

                /// Aggregate the trade positions by period.
                var postions = AggregateTradePositions (date, trades);

                return new Domain.Models.PowerTrade
                    {
                    Date = date,
                    AggregatedPositions = postions
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

        private Dictionary<int, double> AggregateTradePositions ( DateTime date, IEnumerable<PowerTrade> trades )
            {
            var aggregatedPositions = new Dictionary<int, double>();

            foreach ( var trade in trades )
                {

                foreach ( var period in trade.Periods )
                    {
                    if ( aggregatedPositions.ContainsKey (period.Period) )
                        {
                        aggregatedPositions[period.Period] += ( int ) Math.Round (period.Volume);
                        }
                    else
                        {
                        aggregatedPositions[period.Period] = ( int ) Math.Round (period.Volume);
                        }
                    }
                }

            logger.LogDebug (
                "[POWER-SVC] GetTradesAsync │ date={Date:yyyy-MM-dd} │ tradeCount={TradeCount}",
                date, trades.Count ());

            return aggregatedPositions;
            }
        }
    }



