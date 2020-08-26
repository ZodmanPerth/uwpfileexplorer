using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FileExplorerTest
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		public MainPageViewModel ViewModel { get; set; }



		/// Lifecycle



		public MainPage()
		{
			this.InitializeComponent();
			ViewModel = new MainPageViewModel();
			_ = ViewModel.Initialise();
		}



		/// Event Handlers



		async void btnSelectRootFolder_Click(object sender, RoutedEventArgs e)
		{
			var folder = await BrowseForFolder();
			if (folder == null)
				return;

			var fal = StorageApplicationPermissions.FutureAccessList;

			// Ensure we dont exceed maximum number of entries
			if (fal.Entries.Count == fal.MaximumItemsAllowed)
			{
				var oldestEntry = fal.Entries.First(); // Assumes entries are stored oldest first
				fal.Remove(oldestEntry.Token);
			}

			_ = fal.Add(folder, folder.Name);

			ViewModel.RootFolder = folder;

			return;


			/// Local Functions


			async Task<StorageFolder> BrowseForFolder()
			{
				var picker = new FolderPicker
				{
					ViewMode = PickerViewMode.List,
					SuggestedStartLocation = PickerLocationId.DocumentsLibrary
				};
				picker.FileTypeFilter.Add("*");

				var f = await picker.PickSingleFolderAsync();

				return f;
			}
		}

		void lv_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
		{
			ViewModel.DoOpenSelectedFolder();
		}
	}
}
