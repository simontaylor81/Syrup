using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using ICSharpCode.AvalonEdit.Search;
using Microsoft.CodeAnalysis;
using ReactiveUI;
using ShaderEditorApp.View.DocumentEditor;
using ShaderEditorApp.ViewModel.Workspace;

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

		public DocumentView()
		{
			InitializeComponent();

			// Enable Find box.
			SearchPanel.Install(textEditor);

			// Defer various bits of setup until the view model is bound and the Document set on the editor.
			// TODO: Is there a better way to do this, it's a bit messy.
			textEditor.DocumentChanged += TextEditor_DocumentChanged;
		}

		private void TextEditor_DocumentChanged(object sender, EventArgs e)
		{
			var viewModel = (DocumentViewModel)DataContext;

			// Update squigglies and code tips when the diagnostics change.
			var diagnosticsChanged = viewModel.WhenAnyValue(x => x.Diagnostics);

			_squigglyService = new SquigglyService(textEditor.TextArea.TextView,
				diagnosticsChanged.Select(diagnostics => Tuple.Create(viewModel.Document, CreateSquigglies(diagnostics))));
			textEditor.TextArea.TextView.BackgroundRenderers.Add(_squigglyService);

			_codeTipService = new CodeTipService(textEditor, viewModel.CodeTipProvider,
				diagnosticsChanged.Select(diagnostics => Tuple.Create(viewModel.Document, CreateCodeTips(diagnostics))));

			// Get initial set of diagnostics.
			viewModel.GetDiagnostics.ExecuteAsync().Subscribe();
		}

		private IEnumerable<Squiggly> CreateSquigglies(ImmutableArray<Diagnostic> diagnostics)
		{
			return diagnostics.Select(diagnostic => new Squiggly
			{
				Colour = GetDiagnosticColour(diagnostic.Severity),
				StartOffset = diagnostic.Location.SourceSpan.Start,
				Length = diagnostic.Location.SourceSpan.Length,
			});
		}

		private IEnumerable<CodeTip> CreateCodeTips(ImmutableArray<Diagnostic> diagnostics)
		{
			return diagnostics.Select(diagnostic => new CodeTip
			{
				Contents = diagnostic.GetMessage(),
				StartOffset = diagnostic.Location.SourceSpan.Start,
				Length = diagnostic.Location.SourceSpan.Length,
			});
		}

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
