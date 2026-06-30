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
        public async Task WriteAsync_ZeroPositions_ThrowsInvalidOperationExceptionAndWritesNoFile ( )
            {
            var result = new DomainPowerTrade { Date = DateTime.UtcNow, AggregatedPositions = [] };

            await Assert.ThrowsAsync<InvalidOperationException> (
                ( ) => _writer.WriteAsync (result, CancellationToken.None));

            Assert.False (Directory.Exists (_tempDirectory) && Directory.EnumerateFiles (_tempDirectory).Any ());
            }

        [Fact]
        public async Task WriteAsync_ValidPositions_WritesHeaderAndRowsOrderedByPeriodWithCorrectUtcAndVolume ( )
            {
            var result = new DomainPowerTrade
                {
                Date = new DateTime(2026, 1, 15),
                AggregatedPositions = new Dictionary<int, double> { [2] = 150.5, [1] = 100 }
                };

            var filePath = await _writer.WriteAsync(result, CancellationToken.None);
            var lines = await File.ReadAllLinesAsync (filePath, TestContext.Current.CancellationToken);

            Assert.True (File.Exists (filePath));
            Assert.Equal (3, lines.Length); // header + 2 rows
            Assert.Equal ("Datetime;Volume", lines[0]);
            Assert.Equal ("2026-01-14T23:00:00Z;100", lines[1]); // period 1 written before period 2
            Assert.Equal ("2026-01-15T00:00:00Z;150.5", lines[2]);
            }

        [Fact]
        public async Task WriteAsync_CalledTwiceForSameMinute_OverwritesRatherThanFailing ( )
            {
            var result = new DomainPowerTrade
                {
                Date = new DateTime(2026, 1, 15),
                AggregatedPositions = new Dictionary<int, double> { [1] = 100 }
                };

            var firstPath = await _writer.WriteAsync(result, CancellationToken.None);
            var secondPath = await _writer.WriteAsync(result, CancellationToken.None);

            // Same minute => same target file name; the second write must succeed and overwrite, not throw.
            Assert.Equal (firstPath, secondPath);
            Assert.True (File.Exists (secondPath));
            Assert.Empty (Directory.GetFiles (_tempDirectory, "*.tmp")); // no leftover temp file
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
