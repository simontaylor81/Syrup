using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace ShaderEditorApp.View.DocumentEditor
{
	public class TextEditorHelpers
	{
		#region CaretPosition

		public static TextLocation GetCaretPosition(DependencyObject obj)
		{
			return (TextLocation)obj.GetValue(CaretPositionProperty);
		}

		public static void SetCaretPosition(DependencyObject obj, TextLocation value)
		{
			obj.SetValue(CaretPositionProperty, value);
		}

		// Using a DependencyProperty as the backing store for CaretPosition. This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CaretPositionProperty =
			DependencyProperty.RegisterAttached(
				"CaretPosition",
				typeof(TextLocation),
				typeof(TextEditorHelpers),
				new FrameworkPropertyMetadata(new TextLocation(), OnCaretPositionChanged)
				{
					BindsTwoWayByDefault = true
				});

		// Called when the 'CaretPosition' property changes.
		private static void OnCaretPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var textEditor = d as TextEditor;
			var newValue = (TextLocation)e.NewValue;
			if (textEditor != null && textEditor.TextArea.Caret.Location != newValue)
			{
				textEditor.TextArea.Caret.Location = newValue;

				ScrollToCaret(textEditor);
			}
		}

		// Called when the actual caret in the document moves.
		private static void CaretMoved(object sender, TextEditor textEditor)
		{
			var caret = (Caret)sender;
			SetCaretPosition(textEditor, caret.Location);
		}

		#endregion


		#region Selection

		public static int GetSelectionStart(DependencyObject obj)
		{
			return (int)obj.GetValue(SelectionStartProperty);
		}

		public static void SetSelectionStart(DependencyObject obj, int value)
		{
			obj.SetValue(SelectionStartProperty, value);
		}

		// Using a DependencyProperty as the backing store for SelectionStart. This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SelectionStartProperty =
			DependencyProperty.RegisterAttached(
				"SelectionStart",
				typeof(int),
				typeof(TextEditorHelpers),
				new FrameworkPropertyMetadata(0, OnSelectionStartChanged)
				{
					BindsTwoWayByDefault = true
				});

		// Called when the 'SelectionStart' property changes.
		private static void OnSelectionStartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var textEditor = d as TextEditor;
			if (textEditor != null)
			{
				textEditor.SelectionStart = (int)e.NewValue;
				ScrollToCaret(textEditor);
			}
		}

		public static int GetSelectionLength(DependencyObject obj)
		{
			return (int)obj.GetValue(SelectionLengthProperty);
		}

		public static void SetSelectionLength(DependencyObject obj, int value)
		{
			obj.SetValue(SelectionLengthProperty, value);
		}

		// Using a DependencyProperty as the backing store for SelectionLength. This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SelectionLengthProperty =
			DependencyProperty.RegisterAttached(
				"SelectionLength",
				typeof(int),
				typeof(TextEditorHelpers),
				new FrameworkPropertyMetadata(0, OnSelectionLengthChanged)
				{
					BindsTwoWayByDefault = true
				});

		// Called when the 'SelectionLength' property changes.
		private static void OnSelectionLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var textEditor = d as TextEditor;
			if (textEditor != null)
			{
				textEditor.SelectionLength = (int)e.NewValue;
			}
		}

		// Called when the actual selection in the document moves.
		private static void SelectionChanged(TextEditor textEditor)
		{
			SetSelectionStart(textEditor, textEditor.SelectionStart);
			SetSelectionLength(textEditor, textEditor.SelectionLength);
		}

		#endregion


		public static bool GetEnableCustomTextEditorBindings(DependencyObject obj)
		{
			return (bool)obj.GetValue(EnableCustomTextEditorBindingsProperty);
		}

		public static void SetEnableCustomTextEditorBindings(DependencyObject obj, bool value)
		{
			obj.SetValue(EnableCustomTextEditorBindingsProperty, value);
		}

		// Using a DependencyProperty as the backing store for EnableCustomTextEditorBindings.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty EnableCustomTextEditorBindingsProperty =
			DependencyProperty.RegisterAttached(
				"EnableCustomTextEditorBindings",
				typeof(bool),
				typeof(TextEditorHelpers),
				new PropertyMetadata(false, OnEnableCustomTextEditorBindingsChanged));

		private static void OnEnableCustomTextEditorBindingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var textEditor = d as TextEditor;
			if (textEditor == null)
			{
				return;
			}

			var value = (bool)e.NewValue;

			// Hook up events to update the other properties when the selection/caret/scroll position change.
			if (value)
			{
				textEditor.TextArea.Caret.PositionChanged += (o, _) => CaretMoved(o, textEditor);
				textEditor.TextArea.SelectionChanged += (o, _) => SelectionChanged(textEditor);
			}
			else
			{
				// We don't need to disable this ever, so don't bother with the complixity.
				throw new NotSupportedException("Disabling EnableCustomTextEditorBindings is not supported.");
			}
		}

		// Helper to scroll a text editor to the location of the caret.
		private static void ScrollToCaret(TextEditor textEditor)
		{
			// Bring the new caret position into view.
			// HACK: BringCaretToView will fail if we've only just opened this document,
			// as it does not yet have a width/height/etc. So we schedule it to
			// the dispatcher to give it time to get laid out.
			Dispatcher.CurrentDispatcher.InvokeAsync(() => textEditor.TextArea.Caret.BringCaretToView());
		}
	}
}
