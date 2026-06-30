using Axpo.PowerPositionReporter.Application.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Polly;
using Polly.Registry;

namespace Axpo.PowerPositionReporter.UnitTests.Services
    {
    public class PowerTradeServiceTests
        {
        private static PowerTradeService CreateService (
            Mock<IPowerService> powerServiceMock,
            out Mock<ResiliencePipelineProvider<string>> pipelineProviderMock )
            {
            pipelineProviderMock = new Mock<ResiliencePipelineProvider<string>> ();
            pipelineProviderMock
                .Setup (p => p.GetPipeline<IEnumerable<PowerTrade>> (It.IsAny<string> ()))
                .Returns (ResiliencePipeline<IEnumerable<PowerTrade>>.Empty);

            return new PowerTradeService (
                powerServiceMock.Object,
                pipelineProviderMock.Object,
                NullLogger<PowerTradeService>.Instance);
            }

        [Fact]
        public async Task GetAggregateTradePositionsAsync_MultipleTrades_AggregatesAcrossTrades ( )
            {
            var date = new DateTime(2026, 1, 15);
            var tradeA = PowerTradeFactory.Create(date, 24, 10);
            var tradeB = PowerTradeFactory.Create(date, 24, 5);
            var powerServiceMock = new Mock<IPowerService>();
            powerServiceMock.Setup (p => p.GetTradesAsync (date)).ReturnsAsync ([tradeA, tradeB]);

            var service = CreateService(powerServiceMock, out _);

            var result = await service.GetAggregateTradePositionsAsync(date, CancellationToken.None);

            Assert.Equal (15, result.AggregatedPositions[1]);
            }

        [Fact]
        public async Task GetAggregateTradePositionsAsync_NoTradesReturned_ReturnsEmptyPositions ( )
            {
            var date = new DateTime(2026, 1, 15);
            var powerServiceMock = new Mock<IPowerService>();
            powerServiceMock.Setup (p => p.GetTradesAsync (date)).ReturnsAsync ([]);

            var service = CreateService(powerServiceMock, out _);

            var result = await service.GetAggregateTradePositionsAsync(date, CancellationToken.None);

            Assert.Empty (result.AggregatedPositions);
            Assert.Equal (date, result.Date);
            }

        [Fact]
        public async Task GetAggregateTradePositionsAsync_PowerServiceThrows_ExceptionPropagates ( )
            {
            var date = new DateTime(2026, 1, 15);
            var powerServiceMock = new Mock<IPowerService>();
            powerServiceMock
                .Setup (p => p.GetTradesAsync (date))
                .ThrowsAsync (new PowerServiceException ("downstream failure"));

            var service = CreateService(powerServiceMock, out _);

            await Assert.ThrowsAsync<PowerServiceException> (
                ( ) => service.GetAggregateTradePositionsAsync (date, CancellationToken.None));
            }
        }

    internal static class PowerTradeFactory
        {
        public static PowerTrade Create ( DateTime date, int numberOfPeriods, double volumePerPeriod )
            {
            var trade = PowerTrade.Create(date, numberOfPeriods);
            var periods = (PowerPeriod[])trade.Periods;

            for ( var i = 0 ; i < periods.Length ; i++ )
                {
                periods[i].SetVolume (volumePerPeriod);
                }

            return trade;
            }

        public static PowerTrade Create ( DateTime date, int numberOfPeriods, Func<int, double> volumeForPeriod )
            {
            var trade = PowerTrade.Create(date, numberOfPeriods);
            var periods = (PowerPeriod[])trade.Periods;

            for ( var i = 0 ; i < periods.Length ; i++ )
                {
                periods[i].SetVolume (volumeForPeriod (periods[i].Period));
                }

            return trade;
            }
        }
    }