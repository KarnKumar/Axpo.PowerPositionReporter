using Axpo.PowerPositionReporter.Application.Configurations;
using Axpo.PowerPositionReporter.Application.Services;
using Axpo.PowerPositionReporter.Domain.Interfaces;

using Microsoft.Extensions.Options;
using Moq;

using DomainPowerTrade = Axpo.PowerPositionReporter.Domain.Models.PowerTrade;

namespace Axpo.PowerPositionReporter.UnitTests.Services
    {
    public class PowerPositionReportServiceTests
        {
        private static IOptions<AppSettings> Settings ( int intervalMinutes = 60 ) =>
            Options.Create (new AppSettings { IntervalMinutes = intervalMinutes });

        

        [Fact]
        public async Task RunPowerTradePositionReporter_FirstIteration_RunsImmediatelyWithoutWaitingForInterval ( )
            {
            var aggregated = new DomainPowerTrade { Date = DateTime.UtcNow, AggregatedPositions = new() { [1] = 10 } };
            var tradeServiceMock = new Mock<IPowerTradeService>();
            tradeServiceMock
                .Setup (s => s.GetAggregateTradePositionsAsync (It.IsAny<DateTime> (), It.IsAny<CancellationToken> ()))
                .ReturnsAsync (aggregated);

            var reportWriterMock = new Mock<IReportWriter>();
            reportWriterMock
                .Setup (w => w.WriteAsync (aggregated, It.IsAny<CancellationToken> ()))
                .ReturnsAsync ("report.csv");

            var loggerMock = new Mock<IReportLogger>();
            var cts = new CancellationTokenSource();

            // Cancel as soon as the first write happens, so the loop exits after one immediate run
            // rather than waiting for the (long) configured interval.
            reportWriterMock
                .Setup (w => w.WriteAsync (aggregated, It.IsAny<CancellationToken> ()))
                .Returns (async ( ) =>
                {
                    await cts.CancelAsync ();
                    return "report.csv";
                });

            var service = new PowerPositionReportService(
                tradeServiceMock.Object, reportWriterMock.Object, loggerMock.Object, Settings(intervalMinutes: 1440));

            await service.RunPowerTradePositionReporter (cts.Token);

            reportWriterMock.Verify (
                w => w.WriteAsync (aggregated, It.IsAny<CancellationToken> ()),
                Times.Once);
            }

        [Fact]
        public async Task RunPowerTradePositionReporter_RequestsDayAheadDate ( )
            {
            DateTime? requestedDate = null;
            var aggregated = new DomainPowerTrade { Date = DateTime.UtcNow, AggregatedPositions = new() { [1] = 10 } };
            var tradeServiceMock = new Mock<IPowerTradeService>();
            var cts = new CancellationTokenSource();

            tradeServiceMock
                .Setup (s => s.GetAggregateTradePositionsAsync (It.IsAny<DateTime> (), It.IsAny<CancellationToken> ()))
                .Callback<DateTime, CancellationToken> (( date, _ ) => requestedDate = date)
                .ReturnsAsync (aggregated);

            var reportWriterMock = new Mock<IReportWriter>();
            reportWriterMock
                .Setup (w => w.WriteAsync (It.IsAny<DomainPowerTrade> (), It.IsAny<CancellationToken> ()))
                .Returns (async ( ) =>
                {
                    await cts.CancelAsync ();
                    return "report.csv";
                });

            var loggerMock = new Mock<IReportLogger>();

            var service = new PowerPositionReportService(
                tradeServiceMock.Object, reportWriterMock.Object, loggerMock.Object, Settings(intervalMinutes: 1440));

            await service.RunPowerTradePositionReporter (cts.Token);

            Assert.Equal (DateTime.UtcNow.Date.AddDays (1), requestedDate!.Value.Date);
            }

        [Fact]
        public async Task RunPowerTradePositionReporter_AlreadyCancelledToken_ReturnsWithoutWritingReport ( )
            {
            var tradeServiceMock = new Mock<IPowerTradeService>();
            var reportWriterMock = new Mock<IReportWriter>();
            var loggerMock = new Mock<IReportLogger>();
            var cts = new CancellationTokenSource();
            cts.Cancel ();

            var service = new PowerPositionReportService(
                tradeServiceMock.Object, reportWriterMock.Object, loggerMock.Object, Settings());

            await service.RunPowerTradePositionReporter (cts.Token);

            reportWriterMock.Verify (
                w => w.WriteAsync (It.IsAny<DomainPowerTrade> (), It.IsAny<CancellationToken> ()),
                Times.Never);
            }

        [Fact]
        public async Task RunPowerTradePositionReporter_LogsStartupMessage ( )
            {
            var tradeServiceMock = new Mock<IPowerTradeService>();
            var reportWriterMock = new Mock<IReportWriter>();
            var loggerMock = new Mock<IReportLogger>();
            var cts = new CancellationTokenSource();
            cts.Cancel ();

            var service = new PowerPositionReportService(
                tradeServiceMock.Object, reportWriterMock.Object, loggerMock.Object, Settings());

            await service.RunPowerTradePositionReporter (cts.Token);

            loggerMock.Verify (
                l => l.Info (It.Is<string> (msg => msg.Contains ("Started"))),
                Times.Once);
            }
        }
    }