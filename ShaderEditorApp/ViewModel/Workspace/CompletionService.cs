using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ShaderEditorApp.Model.Editor;
using ShaderEditorApp.Model.Editor.CSharp;
using SRPCommon.Logging;

namespace ShaderEditorApp.ViewModel.Workspace
{
	// A list of completion options.
	public class CompletionList
	{
		public IEnumerable<CompletionItem> Completions { get; }
		public char? TriggerChar { get; }

		public CompletionList(IEnumerable<CompletionItem> completions, char? triggerChar)
		{
			Completions = completions;
			TriggerChar = triggerChar;
		}
	}

	// Class for handling async code completion requests.
	public class CompletionService
	{
		private readonly IDocumentServices _editorServices;
		private readonly ILogger _logger;
		private readonly AutoCancelActionService _actionService;

		private Subject<CompletionList> _completions = new Subject<CompletionList>();
		public IObservable<CompletionList> Completions => _completions;

		private Subject<SignatureHelp> _signatureHelp = new Subject<SignatureHelp>();
		public IObservable<SignatureHelp> SignatureHelp => _signatureHelp;

		public CompletionService(IDocumentServices editorServices, ILogger logger)
		{
			_editorServices = editorServices;
			_logger = logger;
			_actionService = new AutoCancelActionService();
		}

		public void TriggerCompletions(int offset, char? triggerChar)
		{
			// Get completion symbols from editor services.
			_actionService.InvokeAsync(ct => _editorServices.GetCompletions(offset, triggerChar, ct))
				.ContinueWith(task =>
				{
					// Ignore cancelled tasks.
					if (task.IsCanceled) return;

					if (task.IsFaulted)
					{
						// Not sure best thing to do here.
						_logger.LogLine("Unhandled exception getting completions: " + task.Exception.Message);
						return;
					}

					// Convert symbols into something consumable by the view.
					_completions.OnNext(new CompletionList(task.Result, triggerChar));
				});
		}

		public void TriggerSignatureHelp(int offset)
		{
			_actionService.InvokeAsync(ct => _editorServices.GetSignatureHelp(offset, ct))
				.ContinueWith(task =>
				{
					// Ignore cancelled tasks.
					if (task.IsCanceled) return;

					if (task.IsFaulted)
					{
						// Not sure best thing to do here.
						_logger.LogLine("Unhandled exception getting signature help: " + task.Exception.Message);
						return;
					}

					_signatureHelp.OnNext(task.Result);
				});
		}
	}
}
