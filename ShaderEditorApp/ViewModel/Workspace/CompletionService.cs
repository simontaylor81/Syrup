using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ShaderEditorApp.Model.Editor;
using ShaderEditorApp.Model.Editor.CSharp;

namespace ShaderEditorApp.ViewModel.Workspace
{
	// A list of completion options.
	public class CompletionList
	{
		public IEnumerable<CompletionItem> Completions { get; }

		public CompletionList(IEnumerable<CompletionItem> completions)
		{
			Completions = completions;
		}
	}

	// Class for handling code completion requests.
	// Currently C# only.
	internal class CompletionService
	{
		private readonly RoslynDocumentServices _editorServices;
		private CancellationTokenSource _completionCancellation;

		public CompletionService(RoslynDocumentServices editorServices)
		{
			_editorServices = editorServices;
		}

		public async Task<CompletionList> GetCompletions(int offset, char? triggerChar)
		{
			// Cancel any existing completion task.
			_completionCancellation?.Cancel();

			_completionCancellation = new CancellationTokenSource();

			// Get completion symbols from editor services.
			var completions = await _editorServices.GetCompletions(offset, triggerChar, _completionCancellation.Token);

			// Convert symbols into something consumable by the view.
			return new CompletionList(completions);
		}
	}
}
