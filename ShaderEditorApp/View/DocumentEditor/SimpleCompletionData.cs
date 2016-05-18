using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ShaderEditorApp.Model.Editor;

namespace ShaderEditorApp.View.DocumentEditor
{
	class SimpleCompletionData : ICompletionData
	{
		private readonly CompletionItem _item;

		public object Content => _item.DisplayText;
		public object Description => null;
		public ImageSource Image => null;
		public double Priority => 0.0f;
		public string Text => _item.InsertionText;

		public SimpleCompletionData(CompletionItem item)
		{
			_item = item;
		}

		public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
		{
			// Use the starting offset from the item rather than the window
			// as it is adjusted to match the symbol being inserted.
			var length = completionSegment.Length - (_item.StartOffset - completionSegment.Offset);
			textArea.Document.Replace(_item.StartOffset, length, _item.InsertionText);
		}
	}
}
