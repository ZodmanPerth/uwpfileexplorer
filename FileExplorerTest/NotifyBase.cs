using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace FileExplorerTest
{
	public class NotifyBase : INotifyPropertyChanged
	{


		/// Actions


		/// <summary>Set the value of a property to a new value, and raise a property changed event.</summary>
		[DebuggerHidden]
		protected void SetValue<T>(ref T backingStore, T newValue, Action<T> valueChangedCallback = null, [CallerMemberName] string propertyName = null)
		{
			if (Equals(backingStore, newValue))
				return;

			backingStore = newValue;

			valueChangedCallback?.Invoke(newValue);

			try
			{
				RaisePropertyChanged(propertyName);
			}
			catch (Exception ex)
			{
				Debugger.Break();
				Debug.WriteLine($"{ex}\n{propertyName}={newValue}");
			}
		}




		/// INotifyPropertyChanged



		/// <summary>Raised when a property value is changed.</summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>Raise the property change event on a property</summary>
		internal void RaisePropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
