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
using ReactiveUI;
using ShaderEditorApp.ViewModel.Properties;

namespace ShaderEditorApp.View.Properties
{
	/// <summary>
	/// Interaction logic for SimpleScalarPropertyView.xaml
	/// </summary>
	public partial class SimpleScalarPropertyView : UserControl
	{
		public SimpleScalarPropertyView()
		{
			InitializeComponent();
		}

		private void TextBox_KeyDown(object sender, KeyEventArgs e)
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

		private void TextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			// Select contents of the text box when it gets focus (via keyboard).
			((TextBox)sender).SelectAll();
		}
	}
}
