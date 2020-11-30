using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileExplorerTest
{
	public static class ServiceLocator
	{
		public static NaturalLanguageComparer NaturalLanguageComparer { get; private set; } 

		internal static void Initialise()
		{
			NaturalLanguageComparer = new NaturalLanguageComparer();
		}
	}
}
