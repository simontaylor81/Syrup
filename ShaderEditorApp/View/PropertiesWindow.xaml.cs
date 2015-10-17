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

using SlimDX;
using ShaderEditorApp.ViewModel;
using System.Globalization;
using System.IO;
using System.Windows.Markup;
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
	}

	class PropertyValueTemplateSelector : DataTemplateSelector
	{
		// Templates to use for the various types.
		public DataTemplate ScalarTemplate { get; set; }
		public DataTemplate BoolTemplate { get; set; }
		public DataTemplate VectorTemplate { get; set; }
		public DataTemplate MatrixTemplate { get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			// TODO: Can this be handled more generically?

			// Anything that just wants a single text box can use the scalar template.
			if (item is ScalarPropertyViewModel<float> || item is ScalarPropertyViewModel<string> || item is ScalarPropertyViewModel<int> || item is DummyPropertyFloat)
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
