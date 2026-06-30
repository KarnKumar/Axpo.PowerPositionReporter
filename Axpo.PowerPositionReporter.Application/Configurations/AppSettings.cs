using System.ComponentModel.DataAnnotations;

namespace Axpo.PowerPositionReporter.Application.Configurations
    {
    /// <summary>
    /// Application settings for the power position reporter, including CSV report path, interval minutes, and max retry attempts.
    /// </summary>
    public class AppSettings
        {
        public const string SectionName = "PowerPositionReporter";

        [Required]
        public string CsvReportPath { get; init; } = "./csvReport";

        [Range (1, 1440)]
        public int IntervalMinutes { get; init; } = 60;

        /// <summary>Retry attempts for calls to the external trading system (<see cref="IAsyncPowerService"/>).</summary>
        [Range (1, 10)]
        public int MaxRetryAttempts { get; init; } = 3;
        }
    }