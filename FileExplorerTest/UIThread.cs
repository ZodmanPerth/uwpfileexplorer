using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Drawboard
{
	/// <summary>A utility class used for synchronizing the UI thread</summary>
	public static class UIThread
	{

		public static event Action<Exception> UnhandledException;

		static SynchronizationContext _context;


		public static bool IsUIThread
		{
			[DebuggerStepThrough]
			get { return _context != null && _context == SynchronizationContext.Current; }
		}




		[DebuggerStepThrough]
		[DebuggerHidden]
		public static void Initialize(SynchronizationContext context)
		{
			_context = context;
		}

		[DebuggerStepThrough]
		[DebuggerHidden]
		public static void Invoke(Action action, bool ignoreCurrentThread = true)
		{
			if (action == null)
				return;

			if (_context == null || (!ignoreCurrentThread && IsUIThread))
			{
				action();
				return;
			}

			_context.Post
			(
				s =>
				{
					try
					{
						((Action)s)();
					}
					catch (Exception ex)
					{
						UnhandledException?.Invoke(ex);
					}
				},
				action
			);
		}

		[DebuggerStepThrough]
		[DebuggerHidden]
		public static Task InvokeAsync(Action action, bool ignoreCurrentThread = true)
		{
			if (action == null)
				return Task.CompletedTask;

			var t = new TaskCompletionSource<bool>();
			var a = new Action(() =>
			{
				try
				{
					action();
					t.SetResult(true);
				}
				catch (Exception ex)
				{
					t.SetException(ex);
				}
			});

			Invoke(a, ignoreCurrentThread);

			return t.Task;
		}

		[DebuggerStepThrough]
		[DebuggerHidden]
		public static Task InvokeAsync(Func<Task> action, bool ignoreCurrentThread = true)
		{
			if (action == null)
				return Task.CompletedTask;

			var t = new TaskCompletionSource<bool>();
			var a = new Action(async () =>
			{
				try
				{
					await action();
					t.SetResult(true);
				}
				catch (Exception ex)
				{
					t.SetException(ex);
				}
			});

			Invoke(a, ignoreCurrentThread);

			return t.Task;
		}

		[DebuggerStepThrough]
		[DebuggerHidden]
		public static Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> function, bool ignoreCurrentThread = true)
		{
			if (function == null)
				return Task.FromResult(default(TResult));

			var t = new TaskCompletionSource<TResult>();
			var a = new Action(async () =>
			{
				try
				{
					var result = await function();
					t.SetResult(result);
				}
				catch (Exception ex)
				{
					t.SetException(ex);
				}
			});

			Invoke(a, ignoreCurrentThread);

			return t.Task;
		}

		[DebuggerStepThrough]
		[DebuggerHidden]
		public static Task<TResult> InvokeAsync<TResult>(Func<TResult> function, bool ignoreCurrentThread = true)
		{
			if (function == null)
				return Task.FromResult(default(TResult));

			var t = new TaskCompletionSource<TResult>();
			var a = new Action(() =>
			{
				try
				{
					var result = function();
					t.SetResult(result);
				}
				catch (Exception ex)
				{
					t.SetException(ex);
				}
			});

			Invoke(a, ignoreCurrentThread);

			return t.Task;
		}
	}
}