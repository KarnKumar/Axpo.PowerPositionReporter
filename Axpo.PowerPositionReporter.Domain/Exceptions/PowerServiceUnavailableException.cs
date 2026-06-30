namespace Axpo.PowerPositionReporter.Domain.Exceptions
    {
    /// <summary>
    /// Thrown when the underlying power trade service cannot be reached or fails
    /// to return trade data after the configured resilience pipeline is exhausted.
    /// </summary>
    public class PowerServiceUnavailableException : Exception
        {
        public PowerServiceUnavailableException ( string message ) : base (message) { }

        public PowerServiceUnavailableException ( string message, Exception innerException )
            : base (message, innerException) { }
        }
    }
