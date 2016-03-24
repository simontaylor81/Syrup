using System;
using System.Windows;
using System.Windows.Controls;

namespace ShaderEditorApp.View
{
	// Class implementing an attached property for auto-scrolling a text box when adding text to it.
	// Based on this Stack Overflow answer: http://stackoverflow.com/a/12346543
	public static class TextBoxUtils
	{
		public static readonly DependencyProperty AutoScrollProperty = DependencyProperty.RegisterAttached(
			"AutoScroll",
			typeof(bool),
			typeof(TextBoxUtils),
			new PropertyMetadata(false, AutoScrollChanged));

		private static void AutoScrollChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			TextBox tb = sender as TextBox;
			if (tb != null)
			{
				bool autoScroll = (e.NewValue != null) && (bool)e.NewValue;
				if (autoScroll)
				{
					tb.ScrollToEnd();
					tb.TextChanged += TextChanged;
					tb.SelectionChanged += SelectionChanged;
				}
				else
				{
					tb.TextChanged -= TextChanged;
					tb.SelectionChanged -= SelectionChanged;
				}
			}
			else
			{
				throw new InvalidOperationException("The attached AutoScroll property can only be applied to TextBox instances.");
			}
		}

		public static bool GetAutoScroll(TextBox textBox)
		{
			if (textBox == null)
			{
				throw new ArgumentNullException(nameof(textBox));
			}

			return (bool)textBox.GetValue(AutoScrollProperty);
		}

		public static void SetAutoScroll(TextBox textBox, bool autoScroll)
		{
			if (textBox == null)
			{
				throw new ArgumentNullException(nameof(textBox));
			}

			textBox.SetValue(AutoScrollProperty, autoScroll);
		}

		private static void TextChanged(object sender, TextChangedEventArgs e)
		{
			((TextBox)sender).ScrollToEnd();
		}

		private static void SelectionChanged(object sender, RoutedEventArgs e)
		{
			
		}
	}
}