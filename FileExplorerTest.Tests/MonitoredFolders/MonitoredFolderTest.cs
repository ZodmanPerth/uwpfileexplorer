using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FileExplorerTest.Tests.MonitoredFolders
{
	public class MonitoredFolderTest
	{
		[Fact]
		public void AddFolder_AtEnd()
		{
			var orderedItems = new List<string>() { "b", "c" };
			var queryItems = new List<string>() { "b", "c", "n" };

			var result = DoWork(orderedItems, queryItems);

			result.ShouldBe(queryItems);
		}


		List<string> DoWork(List<string> _orderedItems, List<string> queryItems)
		{
			int thisIndex = -1;
			int existingIndex = -1;
			var existingItems = _orderedItems.ToList();


			do
			{
				foreach (var item in queryItems)
				{
					thisIndex++;
					var thisItem = item;

					existingIndex++;
				}
				break;

			} while (true);

			return _orderedItems;
		}
	}
}
