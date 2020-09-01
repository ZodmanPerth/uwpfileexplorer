using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace FileExplorerTest
{
	public class MonitoredFolderItem : NotifyBase, IComparable
	{
		IStorageItem _item;

		public MonitoredFolderItemType Type { get; set; }
		public string Name { get; set; }
		public DateTimeOffset DateCreated { get; set; }

		public string ParentFolderPath { get; set; }
		public string DefaultAppName { get; set; }

		DateTimeOffset _dateModified;
		public DateTimeOffset DateModified
		{
			get { return _dateModified; }
			set { SetValue(ref _dateModified, value); }
		}

		DateTimeOffset _itemDate;
		public DateTimeOffset ItemDate
		{
			get { return _itemDate; }
			set { SetValue(ref _itemDate, value); }
		}

		ulong _size;
		public ulong Size
		{
			get { return _size; }
			set { SetValue(ref _size, value); }
		}

		bool _isPropertiesPopulated;
		/// <summary>True if the properties have been populated at least once</summary>
		public bool IsPropertiesPopulated
		{
			get { return _isPropertiesPopulated; }
			set { SetValue(ref _isPropertiesPopulated, value); }
		}



		/// Lifecycle



		public MonitoredFolderItem(IStorageItem item)
		{
			_item = item ?? throw new ArgumentNullException(nameof(item));

			Type = item is StorageFolder ? MonitoredFolderItemType.Folder : MonitoredFolderItemType.File;
			Name = item.Name;
			DateCreated = item.DateCreated;
			ParentFolderPath = System.IO.Path.GetDirectoryName(item.Path);

			if (item is StorageFile file)
				DefaultAppName = file.DisplayType;
		}

		// NOTE: This constructor was commented out when the PInvoke method was abandoned
		//internal MonitoredFolderItem(MonitoredFolderItemType type, string name)
		//{
		//	Type = type;
		//	Name = name;
		//}



		/// Actions



		public void DoPopulateProperties()
		{
			_ = PopulateProperties();


			return;


			/// Local Functions


			async Task PopulateProperties()
			{
				var properties = await _item.GetBasicPropertiesAsync();

				this.DateModified = properties.DateModified;
				this.Size = properties.Size;
				this.ItemDate = properties.ItemDate;

				IsPropertiesPopulated = true;
			}
		}

		public IStorageItem GetItem()
		{
			return _item;
		}



		/// Overrides



		public override bool Equals(object obj)
		{
			var o = obj as MonitoredFolderItem;
			if (o == null)
				return false;

			if (o.Name != Name)
				return false;
			if (o.Type != Type)
				return false;

			return true;
		}

		public override int GetHashCode()
		{
			var hashCode = Name.GetHashCode();
			hashCode = (hashCode * 397) ^ Type.GetHashCode();
			return hashCode;
		}



		/// IComparable



		public int CompareTo(object obj)
		{
			if (obj == null)
				return 1;

			var o = obj as MonitoredFolderItem;
			if (o == null)
				throw new ArgumentException($"Object must be of type {nameof(MonitoredFolderItem)}");

			if (this.Type != o.Type)
				return this.Type.CompareTo(o.Type);

			return StrCmpLogicalW(this.Name, o.Name);
		}



		/// Interop



		[DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		static extern int StrCmpLogicalW(String x, String y);

	}



	/// Types



	public enum MonitoredFolderItemType { Folder, File }

}
