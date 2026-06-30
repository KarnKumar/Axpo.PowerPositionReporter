namespace Axpo.PowerPositionReporter.Domain.Models
    {
    /// <summary>
    /// class representing a power trade with a date and aggregated positions.
    /// </summary>
    public class PowerTrade
            {
            public DateTime Date { get; set; }

            public required Dictionary<int, double> AggregatedPositions { get; set; }

            }
        }
    
