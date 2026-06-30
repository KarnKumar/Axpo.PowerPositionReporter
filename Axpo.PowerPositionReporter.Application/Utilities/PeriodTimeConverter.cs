namespace Axpo.PowerPositionReporter.Application.Utilities
    {
    public static class PeriodTimeConverter
        {
        private static readonly TimeZoneInfo BerlinTimeZone = ResolveBerlinTimeZone();

        private static TimeZoneInfo ResolveBerlinTimeZone ( )
            {
            try
                {
                // IANA id, works on Linux/macOS and modern Windows (ICU-enabled).
                return TimeZoneInfo.FindSystemTimeZoneById ("Europe/Berlin");
                }
            catch ( TimeZoneNotFoundException )
                {
                // Fallback for older Windows systems using Windows time zone ids.
                return TimeZoneInfo.FindSystemTimeZoneById ("W. Europe Standard Time");
                }
            }

        public static DateTime ToUtc ( DateTime tradeDate, int period )
            {
            var berlinLocalStart = new DateTime(tradeDate.Year, tradeDate.Month, tradeDate.Day, 0, 0, 0, DateTimeKind.Unspecified)
                .AddHours(period - 1);

            // ConvertTimeToUtc already returns a DateTime with Kind = Utc, so no extra SpecifyKind call is needed.
            return TimeZoneInfo.ConvertTimeToUtc (berlinLocalStart, BerlinTimeZone);
            }
        }
    }