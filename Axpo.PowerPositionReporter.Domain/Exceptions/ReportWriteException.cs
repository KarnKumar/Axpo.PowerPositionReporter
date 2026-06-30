namespace Axpo.PowerPositionReporter.Domain.Exceptions
    {
    /// <summary>
    /// Thrown when the CSV report cannot be written to disk (I/O, permissions,
    /// or any other failure during the write/move sequence).
    /// </summary>
    public class ReportWriteException : Exception
        {
        public ReportWriteException ( string message ) : base (message) { }

        public ReportWriteException ( string message, Exception innerException )
            : base (message, innerException) { }
        }
    }
