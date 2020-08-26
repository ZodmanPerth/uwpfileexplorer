using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FileAttributes = System.IO.FileAttributes;

namespace FileExplorerTest
{
	// version 1803 onwards
	// Kudos: https://github.com/microsoft/microsoft-ui-xaml/issues/1465#issuecomment-575987737
	// However, it throws Win32 exceptions that cannot be caught: https://docs.microsoft.com/en-us/answers/questions/8279/why-does-findfirstfileexfromapp-throw-a-win32excep.html
	public static class PInvokeFileAccess
	{
		public static List<MonitoredFolderItem> GetItems(string path)
		{
			var result = new List<MonitoredFolderItem>();

			var watch = Stopwatch.StartNew();

			WIN32_FIND_DATA findDataResult;
			FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;

			int additionalFlags = 0;
			if (Environment.OSVersion.Version.Major >= 6)
			{
				findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
				additionalFlags = FIND_FIRST_EX_LARGE_FETCH;
			}

			IntPtr hFile = FindFirstFileExFromApp(path + "\\*.*", findInfoLevel, out findDataResult, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
			var count = 0;
			if (hFile.ToInt32() != -1)
			{
				do
				{
					if (IsSystemItem(findDataResult.itemName))
						continue;

					// NOTE: This section was commented out when the PInvoke method was abandoned
					//if (((FileAttributes)findDataResult.itemAttributes & FileAttributes.Directory) == FileAttributes.Directory)
					//	result.Add(new MonitoredFolderItem(MonitoredFolderItemType.Folder, findDataResult.itemName));
					//else
					//	result.Add(new MonitoredFolderItem(MonitoredFolderItemType.File, findDataResult.itemName));

					++count;
				} while (FindNextFile(hFile, out findDataResult));

				FindClose(hFile);
			}

			Debug.WriteLine($"count: {count}, ellapsed={watch.ElapsedMilliseconds}");

			return result;


			/// Local Functions


			bool IsSystemItem(string itemName)
			{
				if
				(
					findDataResult.itemName == "." ||
					findDataResult.itemName == ".."
				)
					return true;
				return false;
			}
		}



		/// Imports



		enum FINDEX_INFO_LEVELS
		{
			FindExInfoStandard = 0,
			FindExInfoBasic = 1
		}

		enum FINDEX_SEARCH_OPS
		{
			FindExSearchNameMatch = 0,
			FindExSearchLimitToDirectories = 1,
			FindExSearchLimitToDevices = 2
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		struct WIN32_FIND_DATA
		{
			public uint itemAttributes;
			public System.Runtime.InteropServices.ComTypes.FILETIME creationTime;
			public System.Runtime.InteropServices.ComTypes.FILETIME lastAccessTime;
			public System.Runtime.InteropServices.ComTypes.FILETIME lastWriteTime;
			public uint fileSizeHigh;
			public uint fileSizeLow;
			public uint reserved0;
			public uint reserved1;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string itemName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
			public string alternateFileName;
		}

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern IntPtr FindFirstFileExFromApp(
			string lpFileName,
			FINDEX_INFO_LEVELS fInfoLevelId,
			out WIN32_FIND_DATA lpFindFileData,
			FINDEX_SEARCH_OPS fSearchOp,
			IntPtr lpSearchFilter,
			int dwAdditionalFlags);

		const int FIND_FIRST_EX_LARGE_FETCH = 2;

		[DllImport("api-ms-win-core-file-l1-1-0.dll", CharSet = CharSet.Unicode)]
		static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

		[DllImport("api-ms-win-core-file-l1-1-0.dll")]
		static extern bool FindClose(IntPtr hFindFile);
	}
}