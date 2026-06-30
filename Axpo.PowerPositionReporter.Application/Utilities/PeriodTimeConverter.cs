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
                try
                    {
                    return TimeZoneInfo.FindSystemTimeZoneById ("W. Europe Standard Time");
                    }
                catch ( Exception ex ) when ( ex is TimeZoneNotFoundException or InvalidTimeZoneException )
                    {
                    throw new InvalidOperationException (
                        "Unable to resolve the Berlin/W. Europe time zone on this host. " +
                        "Verify the OS time zone database (tzdata) is installed and not corrupted.", ex);
                    }
                }
            catch ( InvalidTimeZoneException ex )
                {
                throw new InvalidOperationException (
                    "The 'Europe/Berlin' time zone data on this host is corrupted.", ex);
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