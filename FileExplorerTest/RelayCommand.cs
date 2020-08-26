using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FileExplorerTest
{
	public class RelayCommand : RelayCommand<object>
	{
		/// <summary>Creates a new command that can always execute</summary>
		public RelayCommand(Action execute) : base(_ => execute(), _ => true) { }

		/// <summary>Creates a new command with a CanExecute check.  Use Refresh() on this command to prompt it to check again.</summary>
		public RelayCommand(Action execute, Func<bool> canExecute) : base(_ => execute(), _ => canExecute()) { }
	}


	public class RelayCommand<T> : ICommand
	{
		readonly Action<T> _execute;
		readonly Func<T, bool> _canExecute;



		/// Lifecycle



		/// <summary>Creates a new command that can always execute</summary>
		public RelayCommand(Action<T> execute) : this(execute, _ => true) { }

		/// <summary>Creates a new command with a CanExecute check.  Use Refresh() on this command to prompt it to check again.</summary>
		public RelayCommand(Action<T> execute, Func<T, bool> canExecute)
		{
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
			_canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
		}



		/// ICommand



		[DebuggerHidden]
		public bool CanExecute(object parameter)
		{
			return _canExecute((T)parameter);
		}

		public event EventHandler CanExecuteChanged;

		[DebuggerHidden]
		public void Execute(object parameter)
		{
			if (CanExecute(parameter))
				_execute((T)parameter);
		}



		/// Actions



		/// <summary>Prompt the command to check if it can execute</summary>
		public void Refresh()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}

	}

}
