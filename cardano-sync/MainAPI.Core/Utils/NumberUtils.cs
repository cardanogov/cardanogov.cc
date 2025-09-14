namespace MainAPI.Core.Utils
{
    /// <summary>
    /// Utility class for number operations.
    /// </summary>
    public static class NumberUtils
    {
        /// <summary>
        /// Chia một chuỗi số cho một số thập phân và làm tròn đến số chữ số thập phân chỉ định.
        /// </summary>
        /// <param name="value">Chuỗi số cần chia</param>
        /// <param name="divisor">Số chia</param>
        /// <param name="decimals">Số chữ số thập phân cần làm tròn</param>
        /// <returns>Kết quả chia và làm tròn, trả về 0 nếu lỗi</returns>
        public static double DivideAndTruncate(string? value, decimal divisor, int decimals)
        {
            if (string.IsNullOrEmpty(value) || !decimal.TryParse(value, out var num))
                return 0;
            var result = (double)(num / divisor);
            return Math.Round(result, decimals);
        }

        public static string FormatValue<T>(T value, int fixedDigits = 2) where T : struct, IComparable, IFormattable, IConvertible
        {
            const decimal QUADRILLION = 1_000_000_000_000_000m; // 10^15
            const decimal TRILLION = 1_000_000_000_000m;        // 10^12
            const decimal BILLION = 1_000_000_000m;             // 10^9
            const decimal MILLION = 1_000_000m;                 // 10^6
            const decimal THOUSAND = 1_000m;                    // 10^3

            decimal val = Convert.ToDecimal(value);

            if (val >= QUADRILLION) return (val / QUADRILLION).ToString($"F{fixedDigits}") + "Q";
            if (val >= TRILLION) return (val / TRILLION).ToString($"F{fixedDigits}") + "T";
            if (val >= BILLION) return (val / BILLION).ToString($"F{fixedDigits}") + "B";
            if (val >= MILLION) return (val / MILLION).ToString($"F{fixedDigits}") + "M";
            if (val >= THOUSAND) return (val / THOUSAND).ToString($"F{fixedDigits}") + "K";
            return val.ToString();
        }

        public static string ConvertTimestampToDate(long timestampInSeconds)
        {
            // Convert seconds to milliseconds and create UTC DateTime
            DateTime date = DateTimeOffset.FromUnixTimeSeconds(timestampInSeconds).UtcDateTime;

            // Format date components with leading zeros
            string day = date.Day.ToString().PadLeft(2, '0');
            string month = date.Month.ToString().PadLeft(2, '0');
            int year = date.Year;
            string hours = date.Hour.ToString().PadLeft(2, '0');
            string minutes = date.Minute.ToString().PadLeft(2, '0');

            return $"{day}/{month}/{year} {hours}:{minutes}";
        }
    }
}