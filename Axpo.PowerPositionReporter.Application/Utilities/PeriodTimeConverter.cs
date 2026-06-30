namespace Axpo.PowerPositionReporter.Application.Utilities
    {
    public static class PeriodTimeConverter
        {
        private static readonly TimeZoneInfo BerlinTimeZone = ResolveBerlinTimeZone();

        private static TimeZoneInfo ResolveBerlinTimeZone ( )
            {
            try
                {
                return TimeZoneInfo.FindSystemTimeZoneById ("Europe/Berlin");
                }
            catch ( TimeZoneNotFoundException )
                {
                return TimeZoneInfo.FindSystemTimeZoneById ("W. Europe Standard Time");
                }
            }

        public static DateTime ToUtc ( DateTime tradeDate, int period )
            {
            var berlinLocalStart = new DateTime(tradeDate.Year, tradeDate.Month, tradeDate.Day, 0, 0, 0, DateTimeKind.Unspecified)
                .AddHours(period - 1);

            var utc = TimeZoneInfo.ConvertTimeToUtc(berlinLocalStart, BerlinTimeZone);

            return DateTime.SpecifyKind (utc, DateTimeKind.Utc);
            }
        }
    }
