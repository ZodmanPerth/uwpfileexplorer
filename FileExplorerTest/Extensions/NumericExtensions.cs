using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
	// Kudos: https://stackoverflow.com/a/48467634/117797 (Friendly Bytes Conversion)
	public static class NumericExtensions
	{
		static string[] _byteUnits = new[] { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

		static char DecimalSeparatorChar = CultureInfo.CurrentUICulture.NumberFormat.CurrencyDecimalSeparator[0];                   // Assumes not empty
		static char GroupSeparatorChar = CultureInfo.CurrentUICulture.NumberFormat.NumberGroupSeparator[0];                         // Assumes not empty
		static string DecimalSeparatorString = CultureInfo.CurrentUICulture.NumberFormat.CurrencyDecimalSeparator[0].ToString();    // Assumes not empty
		static string GroupSeparatorString = CultureInfo.CurrentUICulture.NumberFormat.NumberGroupSeparator[0].ToString();          // Assumes not empty

		public static string AsBytesFriendly(this int number, int decimals = 2) => ((double)number).AsBytesFriendly(decimals);
		public static string AsBytesFriendly(this uint number, int decimals = 2) => ((double)number).AsBytesFriendly(decimals);
		public static string AsBytesFriendly(this long number, int decimals = 2) => ((double)number).AsBytesFriendly(decimals);
		public static string AsBytesFriendly(this ulong number, int decimals = 2) => ((double)number).AsBytesFriendly(decimals);
		public static string AsBytesFriendly(this double number, int decimals = 2)
		{
			const double divisor = 1024;

			int unitIndex = 0;
			var sign = number < 0 ? "-" : string.Empty;
			var value = Math.Abs(number);
			double lastValue = number;

			while (value > 1)
			{
				lastValue = value;

				// NOTE
				// The following introduces ever increasing rounding errors, but at these scales we don't care.
				// It also means we don't have to deal with problematic rounding errors due to dividing doubles.
				value = Math.Round(value / divisor, decimals);

				unitIndex++;
			}

			if (value < 1 && number != 0)
			{
				value = lastValue;
				unitIndex--;
			}

			return $"{sign}{value} {_byteUnits[unitIndex]}";
		}

		public static bool IsDigit(this char c) => char.IsDigit(c);
		public static bool IsDecimalSeparator(this char c) => c == DecimalSeparatorChar;
		public static bool IsGroupSeparator(this char c) => c == GroupSeparatorChar;
		public static string CleanseToDigits(this string text) => text.Replace(DecimalSeparatorString, null).Replace(GroupSeparatorString, null);
		public static string CleanseGroupSeparators(this string text) => text.Replace(GroupSeparatorString, null);
		public static bool IsWhiteSpace(this char c) => char.IsWhiteSpace(c);
	}
}
