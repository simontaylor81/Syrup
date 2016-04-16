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

		public TextSegmentCollection<CodeTip> _tips;
		private readonly ICodeTipProvider _tipProvider;

		private class DummyTipProvider : ICodeTipProvider
		{
			public async Task<string> GetCodeTipAsync(int offset, CancellationToken cancellationToken)
			{
				try
				{
					Console.WriteLine("Getting tip...");
					await Task.Delay(500, cancellationToken);
					Console.WriteLine("Got tip.");
					return "Awesome code!";
				}
				catch (TaskCanceledException)
				{
					Console.WriteLine("Canceled");
					throw;
				}
			}
		}

		private class TipWithPosition
		{
			public string Contents { get; set; }
			public TextViewPosition Position { get; set; }
		}

		public CodeTipService(TextEditor textEditor, ICodeTipProvider tipProvider)
		{
			_tipProvider = tipProvider;
			_textEditor = textEditor;

			/*
			var mouseHover = Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(h => textEditor.MouseHover += h, h => textEditor.MouseHover -= h);
			var mouseHoverStopped = Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(h => textEditor.MouseHoverStopped += h, h => textEditor.MouseHoverStopped -= h);

			var cancel = mouseHover.Merge(mouseHoverStopped);

			// Get new tips when hovering.
			var positions = mouseHover
				.Select(e => GetTextPosition(e.EventArgs))
				.WhereHasValue()
				.Select(position => new { position, offset = GetOffset(position) });

			var providerTips = positions
				.Select(location => Observable.FromAsync(ct => tipProvider.GetCodeTipAsync(location.offset, ct))
					.Select(tip => new TipWithPosition { Contents = tip, Position = location.position }));

			//var diagnosticTips = positions
			//	.Select(pos => )

			// Clear tips when stopped hovering.
			var clearTips = mouseHoverStopped
				.Select(x => Observable.Return<TipWithPosition>(null));

			// Combine new tip and clear tip observables, switch on it and null-subscribe.
			// This unsubscribes to any previous tip task when either asking for a new tip
			// or clearing the tip, ensuring that the underlying task is cancelled.
			providerTips.Merge(clearTips).Switch().Subscribe();

			//tipStream.Subscribe(tip => Console.WriteLine(tip?.Contents));
			providerTips
				.Switch()
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(SetToolTip);
			*/

			_textEditor.MouseHover += OnMouseHover;
			_textEditor.MouseHoverStopped += OnMouseHoverStopped;

			_toolTip = new ToolTip();
			ToolTipService.SetInitialShowDelay(_toolTip, 0);
			_toolTip.PlacementTarget = _textEditor;
			_toolTip.Placement = PlacementMode.Relative;

			 _tips = new TextSegmentCollection<CodeTip>(textEditor.Document);
		}

		private void SetToolTip(TipWithPosition tip)
		{
			if (tip != null)
			{
				// Position the tooltip at the line base instead of the mouse cursor.
				var textView = _textEditor.TextArea.TextView;
				var basePosition = textView.GetVisualPosition(tip.Position, VisualYPosition.LineBottom) - textView.ScrollOffset;

				_toolTip.Content = tip.Contents;
				_toolTip.PlacementRectangle = new Rect(basePosition, basePosition);
				_toolTip.IsOpen = true;
			}
			else
			{
				_toolTip.IsOpen = false;
			}
		}

		public void SetTips(IEnumerable<CodeTip> tips)
		{
			_tips.Clear();
			foreach (var tip in tips)
			{
				_tips.Add(tip);
			}
		}

		// Convert mouse coords to text position.
		private int GetOffset(TextViewPosition position)
		{
			return _textEditor.Document.GetOffset(position.Location);
		}

		// Convert mouse coords to text position.
		private TextViewPosition? GetTextPosition(MouseEventArgs e)
		{
			var textView = _textEditor.TextArea.TextView;
			var position = textView.GetPositionFloor(e.GetPosition(textView) + textView.ScrollOffset);
			if (!position.HasValue || position.Value.Location.IsEmpty)
			{
				// Mouse is not at a valid text position.
				return null;
			}

			return position;
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
			var relaventTips = _tips.FindSegmentsContaining(offset);

			if (relaventTips.Any())
			{
				// Show diagnostic tips straight away.
				ShowTip(CombineTips(relaventTips), position.Value);
			}

			_outstandingRequest = new CancellationTokenSource();

			try
			{
				// Request tips from the provider.
				var providerTip = await _tipProvider.GetCodeTipAsync(offset, _outstandingRequest.Token);
				if (providerTip != null)
				{
					ShowTip(CombineTips(relaventTips, providerTip), position.Value);
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
		private string CombineTips(IEnumerable<CodeTip> diagnosticTips, string providerTip = null)
		{
			// Maybe want some formatting here?

			var tips = diagnosticTips.Select(x => x.Contents);

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
