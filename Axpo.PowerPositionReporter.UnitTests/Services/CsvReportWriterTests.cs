using Axpo.PowerPositionReporter.Application.Configurations;
using Axpo.PowerPositionReporter.Application.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using DomainPowerTrade = Axpo.PowerPositionReporter.Domain.Models.PowerTrade;

namespace Axpo.PowerPositionReporter.UnitTests.Services
    {
    public class CsvReportWriterTests : IDisposable
        {
        private readonly string _tempDirectory;
        private readonly CsvReportWriter _writer;

        public CsvReportWriterTests ( )
            {
            _tempDirectory = Path.Combine (Path.GetTempPath (), $"csv-writer-tests-{Guid.NewGuid ():N}");

            var settings = Options.Create(new AppSettings { CsvReportPath = _tempDirectory });
            _writer = new CsvReportWriter (settings, NullLogger<CsvReportWriter>.Instance);
            }

        [Fact]
        public async Task WriteAsync_ZeroPositions_ThrowsInvalidOperationException ( )
            {
            var result = new DomainPowerTrade { Date = DateTime.UtcNow, AggregatedPositions = [] };

            await Assert.ThrowsAsync<InvalidOperationException> (
                ( ) => _writer.WriteAsync (result, CancellationToken.None));
            }

        [Fact]
        public async Task WriteAsync_ValidPositions_CreatesFileAtReturnedPath ( )
            {
            var result = new DomainPowerTrade
                {
                Date = new DateTime(2026, 1, 15),
                AggregatedPositions = new Dictionary<int, double> { [1] = 100, [2] = 150 }
                };

            var filePath = await _writer.WriteAsync(result, CancellationToken.None);

            Assert.True (File.Exists (filePath));
            }

        [Fact]
        public async Task WriteAsync_ValidPositions_WritesExpectedRowsOrderedByPeriod ( )
            {
            var result = new DomainPowerTrade
                {
                Date = new DateTime(2026, 1, 15),
                AggregatedPositions = new Dictionary<int, double> { [2] = 150, [1] = 100 }
                };

            var filePath = await _writer.WriteAsync(result, CancellationToken.None);
            var lines = await File.ReadAllLinesAsync (filePath, TestContext.Current.CancellationToken);

            Assert.Equal (3, lines.Length); // header + 2 rows
            Assert.Contains ("2026-01-14T23:00:00Z", lines[1]); // period 1 written before period 2
            Assert.Contains ("100", lines[1]);
            }

        public void Dispose ( )
            {
            if ( Directory.Exists (_tempDirectory) )
                {
                Directory.Delete (_tempDirectory, recursive: true);
                }
            }
        }
    }