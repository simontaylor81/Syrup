using System;
using System.Collections.Generic;
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
using SRPCommon.Logging;
using SRPCommon.Util;

namespace ShaderEditorApp
{
	/// <summary>
	/// Interaction logic for OutputWindow.xaml
	/// </summary>
	/// TODO: MVVM- & Rx-ify
	public partial class OutputWindow : UserControl, ILoggerFactory
	{
		public OutputWindow()
		{
			InitializeComponent();
		}

		// Add a message to the output window.
		private void Log(string category, string text)
		{
			// This function can be called on any thread, but we need to access the UI on the UI thread,
			// so post back to the WPF dispatcher.
			Application.Current.Dispatcher.InvokeAsync(() =>
				{
					var outputText = GetOrCreateCategoryTextBox(category);
					bool bAutoScroll = outputText.CaretIndex == outputText.Text.Length;

					outputText.AppendText(text);

					// If the cursor is at the end of the text, automatically scroll to show the new content.
					if (bAutoScroll)
					{
						outputText.ScrollToEnd();
						outputText.CaretIndex = outputText.Text.Length;
					}

					// Make the logged-to text box active so you can see the output.
					CurrentCategory = category;
				});
		}

		// Clear the output for a category.
		private void Clear(string category)
		{
			Application.Current.Dispatcher.InvokeAsync(() =>
				{
					// Don't create a text box just to clear it.
					categoryTextBoxes.GetOrDefault(category)?.Clear();
				});
		}

		private void clearButton_Click(object sender, RoutedEventArgs e)
		{
			// Clear the current category's text box.
			CurrentTextbox?.Clear();
		}

		// Get the text box for a category. Adds one if it doesn't exist.
		private TextBox GetOrCreateCategoryTextBox(string category)
		{
			return categoryTextBoxes.GetOrAdd(category, () =>
			{
				// Add to combo box.
				categoryCombo.Items.Add(category);

				// Create text box. Properties automatically come from style in XAML.
				var textBox = new TextBox();

				// Add to control.
				grid.Children.Add(textBox);

				return textBox;
			});
		}

		// Array of text box controls, one for each category.
		// We have a separate text box for each one so it each maintains its own state, like scroll position.
		private Dictionary<string, TextBox> categoryTextBoxes = new Dictionary<string, TextBox>();

		// Get/set the currently displayed category.
		private string CurrentCategory
		{
			// Just mirror the selection index of the combo box.
			// Setting triggers SelectionChanged event, as desired.
			get { return (string)categoryCombo.SelectedValue; }
			set
			{
				if (CurrentCategory != value)
				{
					categoryCombo.SelectedValue = value;

					// HACK: update UI.
					categoryCombo_SelectionChanged(null, null);
				}
			}
		}

		// Get the text box corresponding to the current category.
		private TextBox CurrentTextbox => CurrentCategory != null ? categoryTextBoxes.GetOrDefault(CurrentCategory) : null;

		// Called when the category selection is changed, either in the UI or programmatically.
		private void categoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Hide all text boxes;
			foreach (var kvp in categoryTextBoxes)
			{
				kvp.Value.Visibility = Visibility.Collapsed;
			}

			// Show the current one.
			if (CurrentTextbox != null)
			{
				CurrentTextbox.Visibility = Visibility.Visible;
			}
		}

		public ILogger CreateLogger(string category)
		{
			return new OutputWindowLogger(this, category);
		}

		// Logger that writes to the output window.
		private class OutputWindowLogger : ILogger
		{
			private readonly string _category;
			private readonly OutputWindow _outputWindow;

			public OutputWindowLogger(OutputWindow outputWindow, string category)
			{
				_outputWindow = outputWindow;
				_category = category;
			}

			public void Clear()
			{
				_outputWindow.Clear(_category);
			}

			public void Log(string message)
			{
				_outputWindow.Log(_category, message);
			}
		}
	}
}
