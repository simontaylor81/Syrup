using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShaderEditorApp.ViewModel.Workspace
{
	// Simple helper class for handling cancellable actions.
	// When invoking a new action, the previous one is cancelled.
	internal class AutoCancelActionService
	{
		private CancellationTokenSource _completionCancellation;

		public async Task<T> InvokeAsync<T>(Func<CancellationToken, Task<T>> action)
		{
			// Cancel any existing task.
			_completionCancellation?.Cancel();

			_completionCancellation = new CancellationTokenSource();

			// Perform the action.
			return await action(_completionCancellation.Token);
		}
	}
}
