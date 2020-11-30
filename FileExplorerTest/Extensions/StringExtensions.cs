using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
	public static class StringExtensions
	{
		public static bool IsNullOrWhiteSpace(this string text) => string.IsNullOrWhiteSpace(text);
	}
}
