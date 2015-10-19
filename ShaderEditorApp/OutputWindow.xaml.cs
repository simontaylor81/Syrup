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
using SRPCommon.Util;

namespace ShaderEditorApp
{
	/// <summary>
	/// Interaction logic for OutputWindow.xaml
	/// </summary>
	public partial class OutputWindow : UserControl, ILogTarget
	{
		public OutputWindow()
		{
			InitializeComponent();

			// Create per-category text boxes and fill category combo box items.
			foreach (LogCategory category in Enum.GetValues(typeof(LogCategory)))
			{
				// Add to combo box.
				categoryCombo.Items.Add(category.ToString());

				// Create text box. Properties automatically come from style in XAML.
				var textBox = new TextBox();

				// Add to control and list.
				grid.Children.Add(textBox);
				categoryTextBoxes[(int)category] = textBox;
			}
			CurrentCategory = LogCategory.Script;
		}

		// Add a message to the output window.
		public void Log(LogCategory category, string text)
		{
			// This function can be called on any thread, but we need to access the UI on the UI thread,
			// so post back to the WPF dispatcher.
			Application.Current.Dispatcher.InvokeAsync(() =>
				{
					var outputText = categoryTextBoxes[(int)category];
					bool bAutoScroll = outputText.CaretIndex == outputText.Text.Length;

					outputText.AppendText(text);

					// If the cursor is at the end of the text, automatically scroll to show the new content.
					if (bAutoScroll)
					{
						outputText.ScrollToEnd();
						outputText.CaretIndex = outputText.Text.Length;
					}
				});
		}

		private void clearButton_Click(object sender, RoutedEventArgs e)
		{
			// Clear the current category's text box.
			CurrentTextbox.Clear();
		}

		// Array of text box controls, one for each category.
		// We have a separate text box for each one so it each maintains its own state, like scroll position.
		private TextBox[] categoryTextBoxes = new TextBox[Enum.GetNames(typeof(LogCategory)).Length];

		// Get/set the currently displayed category.
		public LogCategory CurrentCategory
		{
			// Just mirror the selection index of the combo box.
			// Setting triggers SelectionChanged event, as desired.
			get { return (LogCategory)categoryCombo.SelectedIndex; }
			set { categoryCombo.SelectedIndex = (int)value; }
		}

		// Get the text box corresponding to the current category.
		private TextBox CurrentTextbox => categoryTextBoxes[(int)CurrentCategory];

		// Called when the category selection is changed, either in the UI or programmatically.
		private void categoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Hide all text boxes;
			foreach (var textBox in categoryTextBoxes)
				textBox.Visibility = Visibility.Collapsed;

			// Show the current one.
			CurrentTextbox.Visibility = Visibility.Visible;
		}
	}
}
