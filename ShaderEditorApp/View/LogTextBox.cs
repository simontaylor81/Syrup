using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ShaderEditorApp.ViewModel;

namespace ShaderEditorApp.View
{
	// TextBox subclass for displaying log output in the output window.
	// Handles our custom auto-scroll behaviour.
	public class LogTextBox : TextBox
	{
		public LogTextBox()
		{
			DataContextChanged += LogTextBox_DataContextChanged;
		}

		private void LogTextBox_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var category = (OutputWindowCategoryViewModel)DataContext;

			category.Messages.Subscribe(Append);
			category.Cleared.Subscribe(_ => Clear());
		}

		private void Append(string msg)
		{
			// If the caret is at the end of the output, auto-scroll to keep the latest content in view.
			bool bAutoScroll = CaretIndex == Text.Length;

			AppendText(msg);

			// If the cursor is at the end of the text, automatically scroll to show the new content.
			if (bAutoScroll)
			{
				ScrollToEnd();
				CaretIndex = Text.Length;
			}
		}
	}
}
