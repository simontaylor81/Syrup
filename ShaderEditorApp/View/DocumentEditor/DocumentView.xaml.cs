using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Search;
using Microsoft.CodeAnalysis;
using ReactiveUI;
using ShaderEditorApp.Model.Editor;
using ShaderEditorApp.View.DocumentEditor;
using ShaderEditorApp.ViewModel.Workspace;
using SRPCommon.Util;

namespace ShaderEditorApp.View
{
	/// <summary>
	/// Interaction logic for DocumentView.xaml
	/// </summary>
	/// This control is just a straight wrapper around the AvalonEdit TextEditor control.
	/// It exists to give us an easy place in code-behind to customise the TextEditor control itself.
	public partial class DocumentView : UserControl
	{
		private CodeTipService _codeTipService;
		private SquigglyService _squigglyService;
		private CompletionWindow _completionWindow;
		private OverloadInsightWindow _overloadInsightWindow;

		private DocumentViewModel ViewModel => (DocumentViewModel)DataContext;

		public DocumentView()
		{
			InitializeComponent();

			// Enable Find box.
			SearchPanel.Install(textEditor);

			// Defer various bits of setup until the view model is bound.
			textEditor.DataContextChanged += TextEditor_DataContextChanged;
		}

		private void TextEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			// Update squigglies and code tips when the diagnostics change.
			var diagnosticsChanged = ViewModel.WhenAnyValue(x => x.Diagnostics);

			_squigglyService = new SquigglyService(textEditor.TextArea.TextView, ViewModel.Document,
				diagnosticsChanged.Select(diagnostics => CreateSquigglies(diagnostics)));
			textEditor.TextArea.TextView.BackgroundRenderers.Add(_squigglyService);

			_codeTipService = new CodeTipService(textEditor, ViewModel.Document, ViewModel.DocumentServices,
				diagnosticsChanged.Select(diagnostics => CreateCodeTips(diagnostics)));

			// Get initial set of diagnostics.
			ViewModel.GetDiagnostics.ExecuteAsync().Subscribe();

			textEditor.TextArea.TextEntered += TextArea_TextEntered;
			textEditor.TextArea.TextEntering += TextArea_TextEntering;

			ViewModel.CompletionService.Completions.ObserveOn(RxApp.MainThreadScheduler).Subscribe(ShowCompletionWindow);
			ViewModel.CompletionService.SignatureHelp.ObserveOn(RxApp.MainThreadScheduler).Subscribe(ShowSignatureHelp);
		}

		private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
		{
			Trace.Assert(e.Text.Length == 1);
			var c = e.Text[0];

			if (ViewModel.DocumentServices.IsSignatureHelpTriggerChar(c))
			{
				ViewModel.TriggerSignatureHelp();
			}
			else if (ViewModel.DocumentServices.IsSignatureHelpEndChar(c))
			{
				_overloadInsightWindow?.Close();
			}
			else
			{
				// Don't show a new completion window if we already have one.
				if (_completionWindow == null)
				{
					ViewModel.TriggerCompletions(c);
				}
			}
		}

		private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
		{
			if (e.Text.Length > 0 && _completionWindow != null)
			{
				if (!ShouldFilterCompletion(e.Text[0]))
				{
					// Whenever a non-letter is typed while the completion window is open,
					// insert the currently selected element.
					_completionWindow.CompletionList.RequestInsertion(e);
				}
			}
			// Do not set e.Handled=true.
			// We still want to insert the character that was typed.
		}

		private void ShowCompletionWindow(ViewModel.Workspace.CompletionList completionList)
		{
			if (_completionWindow != null)
			{
				// Don't show a new completion window if we already have one.
				return;
			}

			if (completionList.Completions.Any())
			{
				// Open completion window
				_completionWindow = new CompletionWindow(textEditor.TextArea);
				_completionWindow.CompletionList.CompletionData.AddRange(completionList.Completions
					.Select(completion => new SimpleCompletionData(completion)));

				// Set initial filter based on typed text.
				if (completionList.TriggerChar.HasValue)
				{
					if (ShouldFilterCompletion(completionList.TriggerChar.Value))
					{
						_completionWindow.CompletionList.SelectItem(completionList.TriggerChar.Value.ToString());

						// The trigger character is already entered, so make sure it is included
						// in the completion window's view of things.
						_completionWindow.StartOffset--;
					}
				}

				_completionWindow.Closed += (o_, e_) => _completionWindow = null;
				_completionWindow.Show();
			}
		}

		private void ShowSignatureHelp(SignatureHelp signatureHelp)
		{
			if (signatureHelp != null)
			{
				_overloadInsightWindow = new OverloadInsightWindow(textEditor.TextArea);
				_overloadInsightWindow.Provider = new OverloadProvider(signatureHelp);
				_overloadInsightWindow.Closed += (o, e) => _overloadInsightWindow = null;
				_overloadInsightWindow.Show();
			}
		}

		// True if the given character should filter the completion window when typed.
		// This is very messy and doesn't really belong here.
		private bool ShouldFilterCompletion(char triggerChar)
		{
			// Is the character a valid identifier character?
			return char.IsLetterOrDigit(triggerChar) || triggerChar == '_';
		}

		// Create squigglies for code diagnostics.
		private IEnumerable<Squiggly> CreateSquigglies(ImmutableArray<Diagnostic> diagnostics) => diagnostics
			.Select(diagnostic => new Squiggly
			{
				Colour = GetDiagnosticColour(diagnostic.Severity),
				StartOffset = diagnostic.Location.SourceSpan.Start,
				Length = diagnostic.Location.SourceSpan.Length,
			});

		// Create code tips for diagnostics.
		private IEnumerable<CodeTip> CreateCodeTips(ImmutableArray<Diagnostic> diagnostics) => diagnostics
			.Select(diagnostic => new CodeTip
			{
				Contents = diagnostic.GetMessage(),
				StartOffset = diagnostic.Location.SourceSpan.Start,
				Length = diagnostic.Location.SourceSpan.Length,
			});

		private Color GetDiagnosticColour(DiagnosticSeverity severity)
		{
			switch (severity)
			{
				case DiagnosticSeverity.Info:
					return Colors.Blue;
				case DiagnosticSeverity.Warning:
					return Colors.Green;

				case DiagnosticSeverity.Hidden:
				case DiagnosticSeverity.Error:
				default:
					return Colors.Red;
			}
		}
	}
}
