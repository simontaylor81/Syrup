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

namespace ShaderEditorApp.View
{
	public class TextEditorHelpers
	{
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

				// Bring the new caret position into view.
				// HACK: BringCaretToView will fail if we've only just opened this document,
				// as it does not yet have a width/height/etc. So we schedule it to
				// the dispatcher to give it time to get laid out.
				Dispatcher.CurrentDispatcher.InvokeAsync(() => textEditor.TextArea.Caret.BringCaretToView());
			}
		}

		// Called when the actual caret in the document moves.
		private static void CaretMoved(object sender, TextEditor textEditor)
		{
			var caret = (Caret)sender;
			SetCaretPosition(textEditor, caret.Location);
		}


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
			}
			else
			{
				// We don't need to disable this ever, so don't bother with the complixity.
				throw new NotSupportedException("Disabling EnableCustomTextEditorBindings is not supported.");
			}
		}
	}
}
