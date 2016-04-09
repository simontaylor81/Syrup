using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.CodeAnalysis.Text;

namespace ShaderEditorApp.ViewModel.Workspace
{
	// Implementation of Roslyn SourceTextContainer for an AvalonEdit IDocument.
	class DocumentSourceTextContainer : SourceTextContainer
	{
		private SourceText _currentText;
		public override SourceText CurrentText => _currentText;

		public override event EventHandler<Microsoft.CodeAnalysis.Text.TextChangeEventArgs> TextChanged;

		public DocumentSourceTextContainer(IDocument document)
		{
			// Seed with existing contents.
			_currentText = SourceText.From(document.Text);

			// Hook up event to forward change events.
			// TODO: Do we need to remove this at some point too?
			document.TextChanged += Document_TextChanged;
		}

		// Forward change events from AvalonEdit to Roslyn.
		private void Document_TextChanged(object sender, ICSharpCode.AvalonEdit.Document.TextChangeEventArgs e)
		{
			// Apply change to the SourceText object.
			var oldText = _currentText;
			var textSpan = new TextSpan(e.Offset, e.RemovalLength);
			_currentText = _currentText.WithChanges(new TextChange(
				textSpan,
				e.InsertedText?.Text));

			// Convert args to Rosyln format.
			var args = new Microsoft.CodeAnalysis.Text.TextChangeEventArgs(
				oldText, _currentText, new TextChangeRange(textSpan, e.InsertionLength));
			TextChanged?.Invoke(this, args);
		}
	}
}
