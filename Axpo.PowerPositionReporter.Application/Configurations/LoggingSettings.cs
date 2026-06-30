namespace Axpo.PowerPositionReporter.Application.Configurations
    {
    public class LoggingSettings
        {
        public string LogDirectory { get; init; } = "./logs";
        public int RetainedFileDays { get; init; } = 30;
        public string OutputTemplate { get; init; } = "[{Timestamp:HH:mm: fff} {Level:u3}] {Message:lj}{NewLine}{Exception}";
        }
    }
