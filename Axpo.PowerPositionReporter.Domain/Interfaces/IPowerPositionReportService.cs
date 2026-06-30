namespace Axpo.PowerPositionReporter.Domain.Interfaces
    {
    public interface IPowerPositionReportService
        {
         Task RunPowerTradePositionReporter ( CancellationToken cancellationToken );
        }
    }
