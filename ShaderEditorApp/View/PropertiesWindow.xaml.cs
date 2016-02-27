using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

using ShaderEditorApp.ViewModel;
using ShaderEditorApp.SampleData;

namespace ShaderEditorApp.View
{
	/// <summary>
	/// Interaction logic for PropertiesWindow.xaml
	/// </summary>
	public partial class PropertiesWindow : UserControl
	{
		public PropertiesWindow()
		{
			InitializeComponent();
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

	class PropertyValueTemplateSelector : DataTemplateSelector
	{
		// Templates to use for the various types.
		public DataTemplate ScalarTemplate { get; set; }
		public DataTemplate BoolTemplate { get; set; }
		public DataTemplate ChoiceTemplate { get; set; }
		public DataTemplate VectorTemplate { get; set; }
		public DataTemplate MatrixTemplate { get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			// TODO: Can this be handled more generically?

			if (item is ChoicePropertyViewModel || item is DummyChoiceProperty)
				return ChoiceTemplate;
			// Anything that just wants a single text box can use the scalar template.
			else if (item is ScalarPropertyViewModel<float> || item is ScalarPropertyViewModel<string> || item is ScalarPropertyViewModel<int> || item is DummyPropertyFloat)
				return ScalarTemplate;
			else if (item is ScalarPropertyViewModel<bool> || item is DummyPropertyBool)
				return BoolTemplate;
			else if (item is VectorPropertyViewModel || item is DummyVectorProperty)
				return VectorTemplate;
			else if (item is MatrixPropertyViewModel || item is DummyMatrixProperty)
				return MatrixTemplate;

			return base.SelectTemplate(item, container);
		}
	}
}
