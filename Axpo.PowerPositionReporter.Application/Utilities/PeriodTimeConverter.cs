namespace Axpo.PowerPositionReporter.Application.Utilities
    {
    /// <summary>
    /// Period Time Converter class to convert trade date and period to UTC DateTime.
    /// </summary>
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

        /// <summary>
        /// Converts the given trade date and period to a UTC DateTime.
        /// </summary>
        /// <param name="tradeDate"></param>
        /// <param name="period"></param>
        /// <returns></returns>
        public static DateTime ToUtc ( DateTime tradeDate, int period )
            {
            var localMidnight = DateTime.SpecifyKind(
                new DateTime (tradeDate.Year, tradeDate.Month, tradeDate.Day),
                DateTimeKind.Unspecified);

            var utcMidnight = TimeZoneInfo.ConvertTimeToUtc(localMidnight, BerlinTimeZone);

            return utcMidnight.AddHours (period - 1);
            }
        }
    }