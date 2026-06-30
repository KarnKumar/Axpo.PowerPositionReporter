namespace Axpo.PowerPositionReporter.Domain.Interfaces
    {
    /// <summary>
    /// interface for logging messages in the Power Position Reporter application.
    /// </summary>
    public interface IReportLogger
        {
        void Debug ( string message );
        void Info ( string message );
        void Warning ( string message );
        void Error ( string message, Exception? ex = null );
        void Fatal ( string message, Exception? ex = null );
        }
    }