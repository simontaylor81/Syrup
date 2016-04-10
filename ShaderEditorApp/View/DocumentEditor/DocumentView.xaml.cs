using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
		private ITextMarkerService _markerService;

		public DocumentView()
		{
			InitializeComponent();

			// Enable Find box.
			SearchPanel.Install(textEditor);

			// Defer various bits of setup until the view model is bound and the Document set on the editor.
			textEditor.DocumentChanged += TextEditor_DocumentChanged;
		}

		private void TextEditor_DocumentChanged(object sender, EventArgs e)
		{
			var viewModel = (DocumentViewModel)DataContext;

			var markerService = new TextMarkerService(viewModel.Document);
			textEditor.TextArea.TextView.BackgroundRenderers.Add(markerService);
			textEditor.TextArea.TextView.LineTransformers.Add(markerService);
			_markerService = markerService;

			// Update squigglies when the diagnostics change.
			viewModel.WhenAnyValue(x => x.Diagnostics).Subscribe(AddDiagnosticMarkers);

			// Get initial set of diagnostics.
			viewModel.GetDiagnostics.ExecuteAsync().Subscribe();
		}

		private void AddDiagnosticMarkers(ImmutableArray<Diagnostic> diagnostics)
		{
			// Remove all existing markers.
			_markerService.RemoveAll(x => true);

			foreach (var diagnostic in diagnostics)
			{
				var marker = _markerService.Create(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);
				marker.MarkerTypes = TextMarkerTypes.SquigglyUnderline;
				marker.MarkerColor = Colors.Red;
			}
		}
	}
}
