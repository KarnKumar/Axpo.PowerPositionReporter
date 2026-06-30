using Axpo.PowerPositionReporter.Application.Services;
using Axpo.PowerPositionReporter.Domain.Exceptions;

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
        public async Task GetAggregateTradePositionsAsync_MultipleTrades_SumsVolumesPerPeriodAcrossTrades ( )
            {
            var date = new DateTime(2026, 1, 15);
            var tradeA = PowerTradeFactory.Create(date, 24, 10);
            var tradeB = PowerTradeFactory.Create(date, 24, 5);
            var powerServiceMock = new Mock<IPowerService>();
            powerServiceMock.Setup (p => p.GetTradesAsync (date)).ReturnsAsync ([tradeA, tradeB]);

            var service = CreateService(powerServiceMock, out _);

            var result = await service.GetAggregateTradePositionsAsync(date, CancellationToken.None);

            Assert.Equal (24, result.AggregatedPositions.Count);
            Assert.Equal (15, result.AggregatedPositions[1]);
            Assert.Equal (15, result.AggregatedPositions[24]);
            }

        [Fact]
        public async Task GetAggregateTradePositionsAsync_FractionalVolumes_RoundsEachTradesVolumeBeforeSumming ( )
            {
            // 10.5 rounds to 10 (banker's rounding) before being added to 2.5 -> rounds to 2; sum must be 12, not 13.
            var date = new DateTime(2026, 1, 15);
            var tradeA = PowerTradeFactory.Create(date, 1, _ => 10.5);
            var tradeB = PowerTradeFactory.Create(date, 1, _ => 2.5);
            var powerServiceMock = new Mock<IPowerService>();
            powerServiceMock.Setup (p => p.GetTradesAsync (date)).ReturnsAsync ([tradeA, tradeB]);

            var service = CreateService(powerServiceMock, out _);

            var result = await service.GetAggregateTradePositionsAsync(date, CancellationToken.None);

            Assert.Equal (13, result.AggregatedPositions[1]);
            }

        [Fact]
        public async Task GetAggregateTradePositionsAsync_PowerServiceThrows_WrapsInPowerServiceUnavailableException ( )
            {
            var date = new DateTime(2026, 1, 15);
            var powerServiceMock = new Mock<IPowerService>();
            var downstreamException = new PowerServiceException("downstream failure");
            powerServiceMock
                .Setup (p => p.GetTradesAsync (date))
                .ThrowsAsync (downstreamException);

            var service = CreateService(powerServiceMock, out _);

            var ex = await Assert.ThrowsAsync<PowerServiceUnavailableException> (
                ( ) => service.GetAggregateTradePositionsAsync (date, CancellationToken.None));

            Assert.Same (downstreamException, ex.InnerException);
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