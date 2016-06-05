using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ShaderEditorApp.SampleData;
using ShaderEditorApp.ViewModel.Properties;

namespace ShaderEditorApp.View.Properties
{
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

			if (item is ChoicePropertyViewModel)
				return ChoiceTemplate;
			// Anything that just wants a single text box can use the scalar template.
			else if (item is ScalarPropertyViewModel<float> || item is ScalarPropertyViewModel<string> || item is ScalarPropertyViewModel<int>)
				return ScalarTemplate;
			else if (item is ScalarPropertyViewModel<bool>)
				return BoolTemplate;
			else if (item is VectorPropertyViewModel)
				return VectorTemplate;
			else if (item is MatrixPropertyViewModel)
				return MatrixTemplate;

			return base.SelectTemplate(item, container);
		}
	}
}
