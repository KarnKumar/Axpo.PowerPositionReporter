using Axpo.PowerPositionReporter.Domain.Models;
namespace Axpo.PowerPositionReporter.Domain.Interfaces
    {
    /// <summary>
    /// power trade service interface to get the power trades for a given date.
    /// </summary>
    public interface IPowerTradeService
        {
        Task<PowerTrade> GetAggregateTradePositionsAsync ( DateTime date,  CancellationToken cancellationToken );
        }
    }
