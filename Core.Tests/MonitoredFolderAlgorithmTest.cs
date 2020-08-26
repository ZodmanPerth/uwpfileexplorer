using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FileExplorerTest.Tests
{
	/// <remarks>
	/// This tests the algorithm for keeping the MonitoredFolders's cache in sync with the real folder.
	/// Rather than testing the MonitoredFolder class itself, it replicates the algorithm here to simplify testing.
	/// </remarks>
	public class MonitoredFolderAlgorithmTest
	{
		#region Data

		List<string> CreateData()
		{
			//var result = CreateFolders(200);
			//var result = CreateFolders(10000);
			var result = new[] { "b", "c" }.ToList();

			result.Sort();
			return result;
		}

		List<string> CreateFolders(int count)
		{
			var result = new List<string>(count);

			var template = "Folder #";
			for (int i = 0; i < count; i++)
				result.Add($"{template}{i}");

			return result;
		}

		#endregion



		/// Add



		[Fact]
		public void Add_AtEnd()
		{
			// Arrange

			var orderedItems = CreateData();

			var queryItems = orderedItems.ToList();
			queryItems.Add("New Folder");
			queryItems.Sort();

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
			result.ShouldBe(queryItems);
		}

		[Fact]
		public void Add_AtStart()
		{
			// Arrange

			var orderedItems = CreateData();

			var queryItems = orderedItems.ToList();
			queryItems.Add("a");
			queryItems.Sort();

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
			result.ShouldBe(queryItems);
		}

		[Fact]
		public void Add_AtTwo()
		{
			// Arrange

			var orderedItems = CreateData();
			orderedItems.Add("a");
			orderedItems.Sort();

			var queryItems = orderedItems.ToList();
			queryItems.Add("a0");
			queryItems.Sort();

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
			result.ShouldBe(queryItems);
		}



		/// Remove



		[Fact]
		public void Remove_AtEnd()
		{
			// Arrange

			var orderedItems = CreateData();
			orderedItems.Add("New Folder");
			orderedItems.Sort();

			var queryItems = CreateData();

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
			result.ShouldBe(queryItems);
		}

		[Fact]
		public void Remove_AtStart()
		{
			// Arrange

			var orderedItems = CreateData();
			orderedItems.Add("a");
			orderedItems.Sort();

			var queryItems = CreateData();

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
			result.ShouldBe(queryItems);
		}

		[Fact]
		public void Remove_AtTwo()
		{
			// Arrange

			var orderedItems = CreateData();
			orderedItems.Add("a");
			orderedItems.Sort();

			var queryItems = orderedItems.ToList();
			queryItems.RemoveAt(1);

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
			result.ShouldBe(queryItems);
		}



		/// Rename Last Item



		[Fact]
		public void RenameLast_ToFirst()
		{
			// Arrange

			var orderedItems = CreateData();
			orderedItems.Add("New Folder");
			orderedItems.Sort();

			var queryItems = CreateData();
			queryItems.Add("a");
			queryItems.Sort();

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
			result.ShouldBe(queryItems);
		}

		[Fact]
		public void RenameLast_ToTwo()
		{
			// Arrange

			var orderedItems = CreateData();
			orderedItems.Add("a");
			orderedItems.Add("New Folder");
			orderedItems.Sort();

			var queryItems = CreateData();
			queryItems.Add("a");
			queryItems.Add("a0");
			queryItems.Sort();

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
			result.ShouldBe(queryItems);
		}

		[Fact]
		public void RenameLast_ToLastBefore()
		{
			// Arrange

			var orderedItems = CreateData();
			orderedItems.Add("New Folder (2)");
			orderedItems.Sort();

			var queryItems = CreateData();
			queryItems.Add("New Folder");
			queryItems.Sort();

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
			result.ShouldBe(queryItems);
		}

		[Fact]
		public void RenameLast_ToLastAfter()
		{
			// Arrange

			var orderedItems = CreateData();
			orderedItems.Add("New Folder");
			orderedItems.Sort();

			var queryItems = CreateData();
			queryItems.Add("New Folder (2)");
			queryItems.Sort();

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
			result.ShouldBe(queryItems);
		}



		/// Rename First Item



		[Fact]
		public void RenameFirst_ToFirstBefore()
		{
			// Arrange

			var orderedItems = CreateData();
			orderedItems.Add("a");
			orderedItems.Sort();

			var queryItems = CreateData();
			queryItems.Add("0");
			queryItems.Sort();

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
			result.ShouldBe(queryItems);
		}

		[Fact]
		public void RenameFirst_ToFirstAfter()
		{
			// Arrange

			var orderedItems = CreateData();
			orderedItems.Add("0a");
			orderedItems.Sort();

			var queryItems = CreateData();
			queryItems.Add("0b");
			queryItems.Sort();

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
			result.ShouldBe(queryItems);
		}

		[Fact]
		public void RenameFirst_ToSecond()
		{
			// Arrange

			var orderedItems = CreateData();
			orderedItems.Add("a");
			orderedItems.Sort();

			var queryItems = CreateData();
			queryItems.Add("bb");
			queryItems.Sort();

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
			result.ShouldBe(queryItems);
		}
		
		[Fact]
		public void RenameFirst_ToLast()
		{
			// Arrange

			var orderedItems = CreateData();
			orderedItems.Add("a");
			orderedItems.Sort();

			var queryItems = CreateData();
			queryItems.Add("New Folder");
			queryItems.Sort();

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
			result.ShouldBe(queryItems);
		}
		
		
		
		/// Rename Second Item



		[Fact]
		public void RenameSecond_ToFirst()
		{
			// Arrange

			var orderedItems = CreateData();
			orderedItems.Add("a");
			orderedItems.Sort();

			var queryItems = orderedItems.ToList();
			queryItems.Remove("b");
			queryItems.Add("0");
			queryItems.Sort();

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
			result.ShouldBe(queryItems);
		}

		[Fact]
		public void RenameSecond_ToSecondBefore()
		{
			// Arrange

			var orderedItems = CreateData();
			orderedItems.Add("a");
			orderedItems.Sort();

			var queryItems = orderedItems.ToList();
			queryItems.Remove("b");
			queryItems.Add("a0");
			queryItems.Sort();

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
			result.ShouldBe(queryItems);
		}

		[Fact]
		public void RenameSecond_ToSecondAfter()
		{
			// Arrange

			var orderedItems = CreateData();
			orderedItems.Add("a");
			orderedItems.Sort();

			var queryItems = orderedItems.ToList();
			queryItems.Remove("b");
			queryItems.Add("b0");
			queryItems.Sort();

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
			result.ShouldBe(queryItems);
		}
		
		[Fact]
		public void RenameSecond_ToLast()
		{
			// Arrange

			var orderedItems = CreateData();
			orderedItems.Add("a");
			orderedItems.Sort();

			var queryItems = orderedItems.ToList();
			queryItems.Remove("b");
			queryItems.Add("New Folder");
			queryItems.Sort();

			// Act
			var result = DoWork(orderedItems, queryItems);

			// Assert
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
					if (existingIndex < existingItems.Count)
					{
						var existingItem = existingItems[existingIndex];
						if (existingItem.Equals(thisItem))
							continue;

						var comparison = existingItem.CompareTo(thisItem);

						// Delete all existingItems at this index < thisItem
						while (comparison < 0)
						{
							existingItems.RemoveAt(existingIndex);
							if (existingItems.Count == existingIndex)
								break;

							existingItem = existingItems[existingIndex];
							comparison = existingItem.CompareTo(thisItem);
						}

						if (existingIndex == existingItems.Count)
							existingItems.Add(thisItem);
						else if (comparison > 0)
							existingItems.Insert(existingIndex, thisItem);
					}
					else
					{
						existingItems.Add(thisItem);
					}
				}

				break;
			} while (true);

			// Any remaining existingItems are removed
			if (existingIndex < existingItems.Count - 1)
			{
				var numberToRemove = existingItems.Count - 1 - existingIndex;
				existingItems.RemoveRange(existingItems.Count - numberToRemove, numberToRemove);
			}

			return existingItems;
		}
	}
}
