using Axpo.PowerPositionReporter.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Xunit;
using DomainPowerTrade = Axpo.PowerPositionReporter.Domain.Models.PowerTrade;

namespace Axpo.PowerPositionReporter.BddTests.Steps
    {
    [Binding]
    public class PowerPositionReportingSteps
        {
        private readonly ScenarioContext _scenarioContext;
        private DomainPowerTrade? _aggregatedPositions;
        private string _filePath = string.Empty;
        private readonly List<DomainPowerTrade> _multipleResults = [];

        public PowerPositionReportingSteps ( ScenarioContext scenarioContext )
            {
            _scenarioContext = scenarioContext;
            }

        private ServiceProvider Provider => _scenarioContext.Get<ServiceProvider> ("ServiceProvider");
        private string CsvReportPath => _scenarioContext.Get<string> ("CsvReportPath");

        [Given (@"the power trade service is available")]
        public void GivenThePowerTradeServiceIsAvailable ( )
            {
            Assert.NotNull (Provider.GetService<IPowerTradeService> ());
            }

        [Given (@"the power position report service is available")]
        public void GivenThePowerPositionReportServiceIsAvailable ( )
            {
            Assert.NotNull (Provider.GetService<IPowerPositionReportService> ());
            }

        [When (@"the day-ahead aggregate trade positions are requested")]
        public async Task WhenTheDayAheadAggregateTradePositionsAreRequested ( )
            {
            var tradeService = Provider.GetRequiredService<IPowerTradeService>();
            var dayAhead = DateTime.UtcNow.Date.AddDays(1);

            _aggregatedPositions = await tradeService.GetAggregateTradePositionsAsync (dayAhead, CancellationToken.None);

            var reportWriter = Provider.GetRequiredService<IReportWriter>();
            _filePath = await reportWriter.WriteAsync (_aggregatedPositions, CancellationToken.None);
            }

        [When (@"the day-ahead aggregate trade positions are requested (\d+) times in a row")]
        public async Task WhenTheDayAheadAggregateTradePositionsAreRequestedNTimesInARow ( int times )
            {
            var tradeService = Provider.GetRequiredService<IPowerTradeService>();
            var dayAhead = DateTime.UtcNow.Date.AddDays(1);

            for ( var i = 0 ; i < times ; i++ )
                {
                var result = await tradeService.GetAggregateTradePositionsAsync(dayAhead, CancellationToken.None);
                _multipleResults.Add (result);
                }
            }

        [When (@"the reporter runs and is cancelled shortly after the first iteration")]
        public async Task WhenTheReporterRunsAndIsCancelledShortlyAfterTheFirstIteration ( )
            {
            var reportService = Provider.GetRequiredService<IPowerPositionReportService>();
            using var cts = new CancellationTokenSource();
            cts.CancelAfter (TimeSpan.FromSeconds (5));

            await reportService.RunPowerTradePositionReporter (cts.Token);
            }

        [Then (@"the result should contain (\d+) hourly positions")]
        public void ThenTheResultShouldContainHourlyPositions ( int expectedCount )
            {
            Assert.NotNull (_aggregatedPositions);
            Assert.Equal (expectedCount, _aggregatedPositions!.AggregatedPositions.Count);
            }

        [Then (@"a CSV report should be generated with (\d+) hourly rows")]
        public async Task ThenACsvReportShouldBeGeneratedWithHourlyRows ( int expectedRows )
            {
            Assert.True (File.Exists (_filePath));
            var lines = await File.ReadAllLinesAsync(_filePath);
            Assert.Equal (expectedRows + 1, lines.Length); // +1 for header
            }

        [Then (@"the generated CSV file name should start with ""([^""]*)""")]
        public void ThenTheGeneratedCsvFileNameShouldStartWith ( string prefix )
            {
            var fileName = Path.GetFileName(_filePath);
            Assert.StartsWith (prefix, fileName);
            }

        [Then (@"every request should return a non-empty result")]
        public void ThenEveryRequestShouldReturnANonEmptyResult ( )
            {
            Assert.All (_multipleResults, result => Assert.NotEmpty (result.AggregatedPositions));
            }

        [Then (@"exactly one report file should exist")]
        public void ThenExactlyOneReportFileShouldExist ( )
            {
            var reportFiles = Directory.GetFiles(CsvReportPath, "PowerPosition_*.csv");
            Assert.Single (reportFiles);
            }
        }
    }