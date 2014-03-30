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
	}

	class PropertyValueTemplateSelector : DataTemplateSelector
	{
		// Templates to use for the various types.
		public DataTemplate ScalarTemplate { get; set; }
		public DataTemplate BoolTemplate { get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			// Anything that just wants a single text box can use the scalar template.
			if (item is ScalarPropertyViewModel<float> || item is ScalarPropertyViewModel<string>)
				return ScalarTemplate;
			else if (item is ScalarPropertyViewModel<bool>)
				return BoolTemplate;
			else if (item is VectorPropertyViewModel<float>)
				return GetVectorPropertyTemplate((VectorPropertyViewModel<float>)item);

			return base.SelectTemplate(item, container);
		}

		private DataTemplate GetVectorPropertyTemplate(VectorPropertyViewModel<float> property)
		{
			// Check the cache first.
			DataTemplate result;
			if (vectorPropertyTemplateCache.TryGetValue(property.NumComponents, out result))
			{
				return result;
			}

			// The only way to really create a DataTemplate programmatically is to generate XAML code and parse it on the fly.

			// Need at least the column for the name.
			string columnDefFormat = "<ColumnDefinition Width=\"{0}*\"/>\n";
			string columnDefs = String.Format(columnDefFormat, property.NumComponents);
			
			// Build column def and text box XAML strings.
			string textBoxes = "";
			for (int i = 0; i < property.NumComponents; i++)
			{
				columnDefs += String.Format(columnDefFormat, 1);
				textBoxes += String.Format("<TextBox Grid.Column=\"{0}\" Text=\"{{Binding [{1}], Mode=TwoWay}}\"/>\n", i + 1, i);
			}

			string xaml = String.Format(
				  @"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
						<Grid>
							<Grid.ColumnDefinitions>
								{0}
							</Grid.ColumnDefinitions>

							<TextBlock Grid.Column=""0"" Text=""{{Binding DisplayName}}""/>
							{1}
						</Grid>
					</DataTemplate>",
					columnDefs, textBoxes);

			// Parse the XAML from the string.
			var template = (DataTemplate)XamlReader.Parse(xaml);

			vectorPropertyTemplateCache[property.NumComponents] = template;
			return template;
		}

		private Dictionary<int, DataTemplate> vectorPropertyTemplateCache = new Dictionary<int, DataTemplate>();
	}
}
