using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ReactiveUI;
using ShaderEditorApp.Model.Editor;
using SRPCommon.Util;

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

		private CancellationTokenSource _outstandingRequest;

		// Tips that are pushed at us by the diagnostic system.
		private TextSegmentCollection<CodeTip> _pushTips;

		// Provider for pulling tips from the compiler infrastructure.
		private readonly ICodeTipProvider _tipProvider;

		public CodeTipService(TextEditor textEditor, ICodeTipProvider tipProvider)
		{
			_tipProvider = tipProvider;
			_textEditor = textEditor;

			// Hook mouse hover events to show/clear tool tip.
			_textEditor.MouseHover += OnMouseHover;
			_textEditor.MouseHoverStopped += OnMouseHoverStopped;

			// Create a single WPF tooltip object that we show and hide as appropriate.
			_toolTip = new ToolTip();
			ToolTipService.SetInitialShowDelay(_toolTip, 0);
			_toolTip.PlacementTarget = _textEditor;
			_toolTip.Placement = PlacementMode.Relative;

			 _pushTips = new TextSegmentCollection<CodeTip>(textEditor.Document);
		}

		// Push tips associated with spans of text.
		public void SetTips(IEnumerable<CodeTip> tips)
		{
			_pushTips.Clear();
			foreach (var tip in tips)
			{
				_pushTips.Add(tip);
			}
		}

		private async void OnMouseHover(object sender, MouseEventArgs e)
		{
			CancelOutstandingRequest();
			e.Handled = true;

			// Convert mouse coords to text position.
			var textView = _textEditor.TextArea.TextView;
			var position = textView.GetPositionFloor(e.GetPosition(textView) + textView.ScrollOffset);
			if (!position.HasValue || position.Value.Location.IsEmpty)
			{
				// Mouse is not at a valid text position.
				return;
			}

			var offset = _textEditor.Document.GetOffset(position.Value.Location);
			var relaventPushTips = _pushTips.FindSegmentsContaining(offset);

			if (relaventPushTips.Any())
			{
				// Show push tips straight away.
				ShowTip(CombineTips(relaventPushTips), position.Value);
			}

			_outstandingRequest = new CancellationTokenSource();

			try
			{
				// Request tips from the provider.
				var providerTip = await _tipProvider.GetCodeTipAsync(offset, _outstandingRequest.Token);
				if (providerTip != null)
				{
					ShowTip(CombineTips(relaventPushTips, providerTip), position.Value);
				}
			}
			catch (TaskCanceledException)
			{
				// Swallow cancellations.
			}
		}

		private void ShowTip(string contents, TextViewPosition position)
		{
			// Position the tooltip at the line base instead of the mouse cursor.
			var textView = _textEditor.TextArea.TextView;
			var basePosition = textView.GetVisualPosition(position, VisualYPosition.LineBottom) - textView.ScrollOffset;

			_toolTip.Content = contents;
			_toolTip.PlacementRectangle = new Rect(basePosition, basePosition);
			_toolTip.IsOpen = true;
		}

		// Combine all tips into a single string.
		private string CombineTips(IEnumerable<CodeTip> pushTips, string providerTip = null)
		{
			// Maybe want some formatting here?

			var tips = pushTips.Select(x => x.Contents);

			if (providerTip != null)
			{
				tips = tips.Concat(EnumerableEx.Return(providerTip));
			}

			return string.Join("\n\n", tips);
		}

		private void OnMouseHoverStopped(object sender, MouseEventArgs e)
		{
			CancelOutstandingRequest();
			_toolTip.IsOpen = false;
			e.Handled = true;
		}

		private void CancelOutstandingRequest()
		{
			if (_outstandingRequest != null)
			{
				_outstandingRequest.Cancel();
				_outstandingRequest = null;
			}
		}
	}
}
