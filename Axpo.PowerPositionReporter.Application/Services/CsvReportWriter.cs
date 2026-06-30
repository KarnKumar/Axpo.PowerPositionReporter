using Axpo.PowerPositionReporter.Application.Configurations;
using Axpo.PowerPositionReporter.Application.Utilities;
using Axpo.PowerPositionReporter.Domain.Interfaces;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;

namespace Axpo.PowerPositionReporter.Application.Services
    {
    /// <summary>
    /// Implementation of the IReportWriter interface that writes reports in CSV format.
    /// </summary>
    public class CsvReportWriter ( IOptions<AppSettings> settings, ILogger<CsvReportWriter> logger ) : IReportWriter
        {

        private readonly AppSettings _settings = settings.Value;
        private static readonly CultureInfo _invariant = CultureInfo.InvariantCulture;

        /// <inheritdoc/>
        public async Task<string> WriteAsync (
            Domain.Models.PowerTrade result,
            CancellationToken cancellationToken = default )
            {
            if ( result is not { AggregatedPositions.Count: > 0 } )
                {
                logger.LogWarning (
                    "[CSV-WRITER] Skipped write │ reason=zero positions in result");
                throw new InvalidOperationException (
                    $"Refusing to write: extraction result has zero positions.");
                }

            var extractionTimeUtc = DateTime.UtcNow;
            var filePath  = BuildFilePath(result, extractionTimeUtc);
            var fileName  = Path.GetFileName(filePath);
            var directory = Path.GetDirectoryName(filePath)!;

            logger.LogDebug (
                     "[CSV-WRITER] Preparing write │ file={FileName} │ dir={Directory} │ rows={RowCount}",
                                                   fileName, directory, result.AggregatedPositions.Count);

            var tempPath = $"{filePath}.{Guid.NewGuid():N}.tmp";

            try
                {
                Directory.CreateDirectory (directory);

                var sb = new StringBuilder();
                sb.AppendLine ("Datetime,Volume");

                foreach ( var position in result.AggregatedPositions.OrderBy (p => p.Key) )
                    {
                    var utcDateTime = PeriodTimeConverter.ToUtc (result.Date, position.Key);
                    var datetime    = utcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", _invariant);
                    var volume      = position.Value.ToString("G", _invariant);
                    sb.Append (datetime).Append (';').AppendLine (volume);
                    }

                await File.WriteAllTextAsync (tempPath, sb.ToString (), Encoding.UTF8, cancellationToken);
                File.Move (tempPath, filePath, overwrite: true);

                var fileSizeKb = new FileInfo(filePath).Length / 1024.0;

                logger.LogInformation (
                      "[CSV-WRITER] report generated │ file={FileName} │ path={FilePath} │ rows={RowCount} │ size={FileSizeKb:F1}KB │ dayAhead={DayAhead:yyyy-MM-dd}",
                       fileName,Path.GetFullPath (filePath),result.AggregatedPositions.Count,fileSizeKb,result.Date);

                return filePath;
                }
            catch ( IOException ex )
                {
                logger.LogError (ex,
                    "[CSV-WRITER] report generation FAILED │ file={FileName} │ reason=IO error │ details={ErrorMessage}",
                    fileName, ex.Message);
                throw;
                }
            catch ( UnauthorizedAccessException ex )
                {
                logger.LogError (ex,
                    "[CSV-WRITER] ✗ Write FAILED │ file={FileName} │ reason=Insufficient permissions │ details={ErrorMessage}",
                    fileName, ex.Message);
                throw;
                }
            catch ( Exception ex ) when ( ex is not OperationCanceledException )
                {
                logger.LogError (ex,
                    "[CSV-WRITER] ✗ Write FAILED │ file={FileName} │ dir={Directory} │ rows={RowCount} │ type={ExceptionType} │ reason={ErrorMessage}",
                    fileName, directory, result.AggregatedPositions.Count,
                    ex.GetType ().Name, ex.Message);
                throw;
                }
            finally
                {
                try { if ( File.Exists (tempPath) ) File.Delete (tempPath); } catch { /* non-fatal */ }
                }
            }

        /// <summary>
        /// Builds the file path for the CSV report based on the extraction result and the time of extraction.
        /// </summary>
        /// <param name="result">The aggregated power trade positions.</param>
        /// <param name="extractionTimeUtc">The UTC timestamp at which this extract was run.</param>
        /// <returns></returns>
        private string BuildFilePath ( Domain.Models.PowerTrade result, DateTime extractionTimeUtc )
            {
            var dayAhead   = result.Date.ToString("yyyyMMdd", _invariant);
            var extraction = extractionTimeUtc.ToString("yyyyMMddHHmm", _invariant);
            return Path.Combine (_settings.CsvReportPath, $"PowerPosition_{dayAhead}_{extraction}.csv");
            }
        }

    }