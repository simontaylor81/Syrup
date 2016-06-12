using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

using ShaderEditorApp.ViewModel.Properties;
using ShaderEditorApp.SampleData;

namespace ShaderEditorApp.View.Properties
{
	/// <summary>
	/// Interaction logic for PropertiesWindow.xaml
	/// </summary>
	public partial class PropertiesWindow : UserControl
	{
		public PropertiesWindow()
		{
			InitializeComponent();

			// TEMP
			//StringBuilder sb = new StringBuilder();
			//var xmlSettings = new System.Xml.XmlWriterSettings
			//{
			//	Indent = true,
			//	IndentChars = "    ",
			//	NewLineOnAttributes = true
			//};
			//var writer = System.Xml.XmlWriter.Create(sb, xmlSettings);

			//var xaml = System.Windows.Markup.XamlWriter.Save(headerItemsControl.Template);
			//Console.WriteLine(xaml);
		}

		private void ScalarPropertyValue_KeyDown(object sender, KeyEventArgs e)
		{
			// Update binding when enter is pressed.
			if (e.Key == Key.Enter)
			{
				var textBox = (TextBox)sender;
				var binding = BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty);

				if (binding != null)
				{
					binding.UpdateSource();
				}

				// Select everything in the box to allow a new value to be typed.
				textBox.SelectAll();
			}
		}

		private void ScalarPropertyValue_GotFocus(object sender, RoutedEventArgs e)
		{
			// Select contents of the text box when it gets focus (via keyboard).
			((TextBox)sender).SelectAll();
		}
	}
}
