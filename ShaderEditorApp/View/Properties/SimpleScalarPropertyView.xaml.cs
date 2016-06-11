using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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

	internal class SimpleScalarPropertyViewFactory : IPropertyViewFactory
	{
		public int Priority => 20;
		public bool IsFullWidth => false;

		public bool SupportsProperty(PropertyViewModel property)
		{
			// Support any type of scalar property.
			return property.GetType().IsGenericType &&
				property.GetType().GetGenericTypeDefinition() == typeof(ScalarPropertyViewModel<>);
		}

		public FrameworkElement CreateView(PropertyViewModel property) => new SimpleScalarPropertyView();
	}
}
