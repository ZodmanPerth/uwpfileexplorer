using NeoSmart.AsyncLock;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace FileExplorerTest
{
	public class MonitoredFolder : IDisposable
	{
		/// <summary>Raised when there are changes in the folder</summary>
		public event EventHandler<MonitoredFolderChangedArgs> Changed;
		public event EventHandler<string> ScanComplete;

		/// <summary>The folder being monitored</summary>
		readonly StorageFolder _monitoredFolder;

		/// <summary>The consumer's event handler</summary>
		readonly EventHandler<MonitoredFolderChangedArgs> _changedEventHandler;

		/// <summary>A simple ID for debug text</summary>
		int _debugScanCount;

		/// <summary>Only tracks whether _rootFolder has changed.  Not used in scans.</summary>
		StorageFileQueryResult _contentChangedMonitor;

		CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
		CancellationToken _cancellationToken;
		AsyncLock _lock = new AsyncLock();

		/// <summary>Used to ensure a maximum of one future scan can be queued</summary>
		bool _isScanQueued;

		/// <summary>The items we've read from the monitored Folder</summary>
		List<MonitoredFolderItem> _cachedItems = new List<MonitoredFolderItem>();



		/// Lifecycle



		public MonitoredFolder(StorageFolder folderToObserve, EventHandler<MonitoredFolderChangedArgs> changedEventHandler)
		{
			_monitoredFolder = folderToObserve ?? throw new ArgumentNullException(nameof(folderToObserve));
			_changedEventHandler = changedEventHandler ?? throw new ArgumentNullException(nameof(changedEventHandler));

			StartMonitoringFolderForChanges();

			// Hookup user's handler for our changed event
			this.Changed += changedEventHandler;

			// Create cancellation token
			_cancellationToken = _cancellationTokenSource.Token;

			ReportLine($"Monitoring folder '{_monitoredFolder.Name}'");

			// Start producing results
			_ = InitialScan();


			return;


			/// Local Functions


			/// <summary>Sets up monitoring _rootFolder for changes. NOTE: Not used for scans.</summary>
			void StartMonitoringFolderForChanges()
			{ 
				var options = new QueryOptions();
				_contentChangedMonitor = _monitoredFolder.CreateFileQueryWithOptions(options);
				_contentChangedMonitor.ContentsChanged += OnContentsChanged;
				_ = _contentChangedMonitor.GetItemCountAsync(); // Starts monitoring for changes
			}


		}

		~MonitoredFolder()
		{
			Dispose(false);
		}



		/// Actions



		async Task InitialScan()
		{
			var stopwatch = Stopwatch.StartNew();

			ReportLine("Started initial scan\n");

			StorageItemQueryResult query = CreateQuery();

			try
			{
				using (await _lock.LockAsync())
				{
					int itemIndex = 0;
					uint chunkIndex = 0;
					uint chunkSize = 50;
					var chunkItems = new List<MonitoredFolderItem>((int)chunkSize);
					int addedCount = 0;

					do
					{
						if (_cancellationToken.IsCancellationRequested)
							break;

						// Get items
						var items = await query.GetItemsAsync(chunkIndex, chunkSize);
						if (!items.Any())
							break;

						Report(".");

						// Gather added items
						chunkItems.Clear();
						foreach (var item in items)
						{
							if (_cancellationToken.IsCancellationRequested)
								break;

							var itemName = item.Name;

							var newItem = new MonitoredFolderItem(item);
							_cachedItems.Insert(itemIndex, newItem);

							chunkItems.Add(newItem);
							itemIndex++;
						}

						if (_cancellationToken.IsCancellationRequested)
							break;

						if (chunkItems.Any())
						{
							addedCount += chunkItems.Count;

							// Raise event
							Changed?.Invoke(this, new MonitoredFolderChangedArgs(chunkItems));
						}

						// Move Next
						chunkIndex += (uint)items.Count();
					}
					while (true);

					stopwatch.Stop();

					ReportLine($"+{addedCount}");
					ReportLine($"Finished initial scan.  Processed {chunkIndex} items in {stopwatch.ElapsedMilliseconds} ms.");

					ReportLine("Releasing Lock");

					// Raise event
					ScanComplete?.Invoke(this, stopwatch.ElapsedMilliseconds.ToString());
				}
			}
			catch (Exception ex)
			{
				ReportLine($"Exception\n{ex}");
			}
		}

		async Task Scan()
		{
			var scanId = ++_debugScanCount;
			ReportLine(scanId, "Started");

			try
			{
				using (await _lock.LockAsync())
				{
					_isScanQueued = false;

					ReportLine(scanId, "Obtained Lock\n");

					var stopwatch = Stopwatch.StartNew();

					var query = CreateQuery();

					uint chunkIndex = 0;
					uint chunkSize = 50;
					var chunkItemsAdded = new List<MonitoredFolderItem>((int)chunkSize);
					var chunkItemsRemoved = new List<MonitoredFolderItem>((int)chunkSize);
					int addedCount = 0;
					int removedCount = 0;

					int thisIndex = -1;
					int existingIndex = -1;
					var existingItems = _cachedItems.ToList();

					do
					{
						if (_cancellationToken.IsCancellationRequested)
							break;

						var items = await query.GetItemsAsync(chunkIndex, chunkSize);
						if (!items.Any())
							break;

						Report(".");

						foreach (var item in items)
						{
							if (_cancellationToken.IsCancellationRequested)
								break;

							thisIndex++;
							var thisItem = new MonitoredFolderItem(item);

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
									chunkItemsRemoved.Add(existingItem);

									if (existingItems.Count == existingIndex)
										break;

									existingItem = existingItems[existingIndex];
									comparison = existingItem.CompareTo(thisItem);
								}

								if (existingIndex == existingItems.Count)
								{
									existingItems.Add(thisItem);
									chunkItemsAdded.Add(thisItem);
								}
								else if (comparison > 0)
								{
									existingItems.Insert(existingIndex, thisItem);
									chunkItemsAdded.Add(thisItem);
								}
							}
							else
							{
								existingItems.Add(thisItem);
								chunkItemsAdded.Add(thisItem);
							}
						}

						if (_cancellationToken.IsCancellationRequested)
							break;

						// Raise event
						if (chunkItemsAdded.Any() || chunkItemsRemoved.Any())
						{
							await UIThread.InvokeAsync(() => { Changed?.Invoke(this, new MonitoredFolderChangedArgs(chunkItemsAdded, chunkItemsRemoved)); });

							addedCount += chunkItemsAdded.Count;
							removedCount += chunkItemsRemoved.Count;

							chunkItemsAdded.Clear();
							chunkItemsRemoved.Clear();
						}

						// Move Next
						chunkIndex += (uint)items.Count();
					}
					while (true);

					// Any remaining existingItems are removed
					if (existingIndex < existingItems.Count - 1)
					{
						var itemsToRemove = existingItems.Where((_, i) => i > existingIndex).ToList();
						chunkItemsRemoved.Clear();
						chunkItemsRemoved.AddRange(itemsToRemove);

						removedCount += itemsToRemove.Count;

						var numberToRemove = existingItems.Count - 1 - existingIndex;
						existingItems.RemoveRange(existingItems.Count - numberToRemove, numberToRemove);

						await UIThread.InvokeAsync(() => { Changed?.Invoke(this, new MonitoredFolderChangedArgs(null, chunkItemsRemoved)); });
					}

					// Result
					_cachedItems = existingItems;

					stopwatch.Stop();

					if (chunkIndex > 0)
						ReportLine($"+{addedCount} -{removedCount}");

					ReportLine(scanId, $"Finished.  Processed {chunkIndex} items in {stopwatch.ElapsedMilliseconds} ms.");

					ReportLine("Releasing Lock");

					// Raise event
					ScanComplete?.Invoke(this, stopwatch.ElapsedMilliseconds.ToString());
				}
			}
			catch (Exception ex)
			{
				ReportLine(scanId, $"Exception\n{ex}");
			}
		}



		/// Event Handlers



		void OnContentsChanged(IStorageQueryResultBase sender, object args)
		{
			if (_isScanQueued)
			{
				ReportLine($"#### ContentsChanged Detected, but a scan is already queued\n");
				return;
			}

			if (_cancellationToken != null && _cancellationToken.IsCancellationRequested)
			{
				ReportLine($"#### ContentsChanged Detected, but the job has been cancelled\n");
				return;
			}

			_isScanQueued = true;
			ReportLine($"#### Queueing another scan\n");
			_ = Scan();
		}



		/// Helpers



		StorageItemQueryResult CreateQuery()
		{
			var fileTypes = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".tif" };
			var options = new QueryOptions(CommonFileQuery.DefaultQuery, fileTypes);
			options.IndexerOption = IndexerOption.UseIndexerWhenAvailable;
			var query = _monitoredFolder.CreateItemQueryWithOptions(options);
			return query;
		}

		void ReportLine(int scanId, string message)
		{
			Debug.Write($"\nScan {scanId}: {message}");
		}

		void Report(string message)
		{
			Debug.Write($"{message}");
		}

		void ReportLine(string message)
		{
			Debug.Write($"\n{message}");
		}



		/// Dispose



		bool _isDisposed;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>Called when the object is being disposed or finalized</summary>
		/// <param name="disposing">
		/// true when the object is being disposed (and therefore can access managed members); 
		/// false when the object is being finalized without first having been disposed (and therefore can only touch unmanaged members).
		/// </param>
		void Dispose(bool disposing)
		{
			if (disposing && !_isDisposed)
			{
				_cancellationTokenSource?.Cancel();

				// Stop monitoring _rootFolder
				_contentChangedMonitor.ContentsChanged -= OnContentsChanged;
				_contentChangedMonitor = null;

				// Disconnect consumer's event handler
				this.Changed -= _changedEventHandler;

				_isDisposed = true;

				ReportLine($"\nNo longer monitoring folder '{_monitoredFolder.Name}'");
			}
		}

	}



	/// Types



	public class MonitoredFolderChangedArgs : EventArgs
	{
		public List<MonitoredFolderItem> NewItems { get; }
		public List<MonitoredFolderItem> OldItems { get; }

		public MonitoredFolderChangedArgs(List<MonitoredFolderItem> newItems, List<MonitoredFolderItem> oldItems = null)
		{
			NewItems = newItems ?? new List<MonitoredFolderItem>();
			OldItems = oldItems ?? new List<MonitoredFolderItem>();
		}
	}
}
