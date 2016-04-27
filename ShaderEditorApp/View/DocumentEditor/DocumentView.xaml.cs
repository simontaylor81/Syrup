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
			var viewModel = (DocumentViewModel)DataContext;

			// Update squigglies and code tips when the diagnostics change.
			var diagnosticsChanged = viewModel.WhenAnyValue(x => x.Diagnostics);

			_squigglyService = new SquigglyService(textEditor.TextArea.TextView, viewModel.Document,
				diagnosticsChanged.Select(diagnostics => CreateSquigglies(diagnostics)));
			textEditor.TextArea.TextView.BackgroundRenderers.Add(_squigglyService);

			_codeTipService = new CodeTipService(textEditor, viewModel.Document, viewModel.DocumentServices,
				diagnosticsChanged.Select(diagnostics => CreateCodeTips(diagnostics)));

			// Get initial set of diagnostics.
			viewModel.GetDiagnostics.ExecuteAsync().Subscribe();

			textEditor.TextArea.TextEntered += TextArea_TextEntered;
			textEditor.TextArea.TextEntering += TextArea_TextEntering;
		}

		private async void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
		{
			if (_completionWindow != null)
			{
				// Don't show a new completion window if we already have one.
				return;
			}

			try
			{
				Trace.Assert(e.Text.Length == 1);
				char triggerChar = e.Text[0];

				var viewModel = (DocumentViewModel)DataContext;
				var completionList = await viewModel.GetCompletions(triggerChar);

				if (completionList.Completions.Any())
				{
					// Open completion window
					_completionWindow = new CompletionWindow(textEditor.TextArea);
					_completionWindow.CompletionList.CompletionData.AddRange(completionList.Completions
						.Select(completion => new SimpleCompletionData(completion)));

					// Set initial filter based on typed text.
					if (ShouldFilterCompletion(triggerChar))
					{
						_completionWindow.CompletionList.SelectItem(e.Text);

						// The trigger character is already entered, so make sure it is included
						// in the completion window's view of things.
						_completionWindow.StartOffset--;
					}

					_completionWindow.Closed += (o_, e_) => _completionWindow = null;
					_completionWindow.Show();
				}
			}
			catch (OperationCanceledException)
			{
				// Ignore cancelled tasks.
			}
		}


		void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
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
