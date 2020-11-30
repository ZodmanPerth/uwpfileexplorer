using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace FileExplorerTest
{
	public class MainPageViewModel : NotifyBase
	{
		MonitoredFolder _monitoredFolder;

		StorageFolder _rootFolder;
		/// <summary>The folder at the root of the folder hierarchy</summary>
		public StorageFolder RootFolder
		{
			get => _rootFolder;
			set
			{
				SetValue(ref _rootFolder, value);
				CurrentFolder = value;
			}
		}

		StorageFolder _currentFolder;
		/// <summary>The current folder being viewed</summary>
		public StorageFolder CurrentFolder
		{
			get => _currentFolder;
			set => SetValue(ref _currentFolder, value);
		}

		/// <summary>The items in the root folder</summary>
		public ObservableCollection<MonitoredFolderItem> Items { get; set; }

		MonitoredFolderItem _selectedItem;
		/// <summary>The selected item in the list</summary>
		public MonitoredFolderItem SelectedItem
		{
			get => _selectedItem;
			set
			{
				SetValue(ref _selectedItem, value);
				RaisePropertyChanged(nameof(IsItemSelected));
				RaisePropertyChanged(nameof(IsFileSelected));
				RaisePropertyChanged(nameof(IsFolderSelected));
			}
		}

		public bool IsItemSelected => _selectedItem != null;
		public bool IsFileSelected => _selectedItem?.Type == MonitoredFolderItemType.File;
		public bool IsFolderSelected => _selectedItem?.Type == MonitoredFolderItemType.Folder;

		string _debugText;
		public string DebugText
		{
			get => _debugText;
			set => SetValue(ref _debugText, value);
		}

		public RelayCommand CommandNavigateUpOneFolder { get; }



		/// Lifecycle



		public MainPageViewModel()
		{
			Items = new ObservableCollection<MonitoredFolderItem>();

			CommandNavigateUpOneFolder = new RelayCommand(DoNavigateUpOneFolder, () => _rootFolder?.Path != _currentFolder?.Path);

			PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(CurrentFolder))
				{
					DoMonitorCurrentFolder();
					CommandNavigateUpOneFolder.Refresh();
				}
				else if (e.PropertyName == nameof(SelectedItem))
					_selectedItem?.DoPopulateProperties();
			};
		}

		public async Task Initialise()
		{
			await RestoreLastRootFolder();

			return;


			/// Local Functions


			async Task RestoreLastRootFolder()
			{
				var fal = StorageApplicationPermissions.FutureAccessList;
				foreach (var entry in fal.Entries)
				{
					var item = await fal.GetItemAsync(entry.Token);
					if (item is StorageFolder folder)
					{
						RootFolder = folder;
						break;
					}
				}
			}
		}



		/// Actions



		public void DoOpenSelectedFolder()
		{
			if (_selectedItem == null)
				return;
			if (_selectedItem.Type != MonitoredFolderItemType.Folder)
				return;

			CurrentFolder = SelectedItem.GetItem() as StorageFolder;
		}

		void DoMonitorCurrentFolder()
		{
			Items.Clear();

			if (_monitoredFolder != null)
			{
				_monitoredFolder.ScanComplete -= DoScanCompleted;
				_monitoredFolder.Dispose();
				_monitoredFolder = null;
			}

			if (_currentFolder == null)
				return;

			Items.Clear();

			_monitoredFolder = new MonitoredFolder(_currentFolder, OnMonitoredFolderChanged);
			_monitoredFolder.ScanComplete += DoScanCompleted;

			return;


			/// Local Functions


			void DoScanCompleted(object sender, string s) => UIThread.InvokeAsync(() => DebugText = $"Folder scan took {s}ms");
		}


		async void DoNavigateUpOneFolder()
		{
			var parent = await _currentFolder.GetParentAsync();
			CurrentFolder = parent;
		}



		/// Event Handlers




		void OnMonitoredFolderChanged(object sender, MonitoredFolderChangedArgs e)
		{
			if (e.NewItems.Any())
				foreach (MonitoredFolderItem addedItem in e?.NewItems)
				{
					var index = FindAddIndex(addedItem);
					Items.Insert(index, addedItem);
				}

			if (e.OldItems.Any())
				foreach (MonitoredFolderItem removedItems in e.OldItems)
					Items.Remove(removedItems);
		}



		/// Helpers



		int FindAddIndex(MonitoredFolderItem itemToAdd)
		{
			for (int i = Items.Count - 1; i >= 0; i--)
				if (itemToAdd.CompareTo(Items[i]) >= 0)
					return i + 1;

			return 0;
		}
	}
}
