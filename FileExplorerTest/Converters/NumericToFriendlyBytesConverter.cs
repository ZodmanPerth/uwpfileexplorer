using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace FileExplorerTest.Converters
{
	public class NumericToFriendlyBytesConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is int i)
				return i.AsBytesFriendly();

			if (value is int ui)
				return ui.AsBytesFriendly();

			if (value is long l)
				return l.AsBytesFriendly();

			if (value is ulong ul)
				return ul.AsBytesFriendly();

			if (value is double d)
				return d.AsBytesFriendly();

			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
