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

			_codeTipService = new CodeTipService(textEditor, viewModel.Document, viewModel.CodeTipProvider,
				diagnosticsChanged.Select(diagnostics => CreateCodeTips(diagnostics)));

			// Get initial set of diagnostics.
			viewModel.GetDiagnostics.ExecuteAsync().Subscribe();
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
