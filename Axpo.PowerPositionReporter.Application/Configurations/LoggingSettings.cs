namespace Axpo.PowerPositionReporter.Application.Configurations
    {
    /// <summary>
    /// Settings controlling file and console logging behavior for the power position reporter.
    /// </summary>
    public class LoggingSettings
        {
        public string LogDirectory { get; init; } = "./logs";

        public int RetainedFileDays { get; init; } = 30;

        public string OutputTemplate { get; init; } = "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}";
        }
    }