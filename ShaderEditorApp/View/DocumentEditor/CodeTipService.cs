using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ShaderEditorApp.View.DocumentEditor
{
	// Simple container with details about a potential tool tip.
	// Flesh this out a bit?
	sealed class CodeTip : TextSegment
	{
		public string Contents { get; set; }
	}

	// Class for handling editor tool tips (errors, type info, etc.)
	class CodeTipService
	{
		private TextEditor _textEditor;
		private ToolTip _toolTip;

		public TextSegmentCollection<CodeTip> _tips;

		public CodeTipService(TextEditor textEditor)
		{
			_textEditor = textEditor;
			_textEditor.MouseHover += OnMouseHover;
			_textEditor.MouseHoverStopped += OnMouseHoverStopped;

			_toolTip = new ToolTip();
			ToolTipService.SetInitialShowDelay(_toolTip, 0);
			_toolTip.PlacementTarget = _textEditor;
			_toolTip.Placement = PlacementMode.Relative;

			 _tips = new TextSegmentCollection<CodeTip>(textEditor.Document);
		}

		public void SetTips(IEnumerable<CodeTip> tips)
		{
			_tips.Clear();
			foreach (var tip in tips)
			{
				_tips.Add(tip);
			}
		}

		private void OnMouseHover(object sender, MouseEventArgs e)
		{
			// Convert mouse coords to text position.
			var textView = _textEditor.TextArea.TextView;
			var position = textView.GetPositionFloor(e.GetPosition(textView) + textView.ScrollOffset);
			if (!position.HasValue || position.Value.Location.IsEmpty)
			{
				// Mouse is not at a valid text position.
				return;
			}

			var offset = _textEditor.Document.GetOffset(position.Value.Location);
			var relaventTips = _tips.FindSegmentsContaining(offset);

			if (!relaventTips.Any())
			{
				// No tips at this location.
				return;
			}

			// Combine all tips into a single string.
			// Maybe want some formatting here?
			var contents = string.Join("\n\n", relaventTips.Select(x => x.Contents));

			// Position the tooltip at the line base instead of the mouse cursor.
			var basePosition = textView.GetVisualPosition(position.Value, VisualYPosition.LineBottom) - textView.ScrollOffset;

			_toolTip.Content = contents;
			_toolTip.PlacementRectangle = new Rect(basePosition, basePosition);
			_toolTip.IsOpen = true;

			e.Handled = true;
		}

		private void OnMouseHoverStopped(object sender, MouseEventArgs e)
		{
			_toolTip.IsOpen = false;
			e.Handled = true;
		}
	}
}
