using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ShaderEditorApp.ViewModel.Properties;

namespace ShaderEditorApp.View.Properties
{
	class FullWidthPropertyTemplateSelector : DataTemplateSelector
	{
		public DataTemplate TwoColumnTemplate { get; set; }
		public DataTemplate TwoColumnCompositeTemplate { get; set; }
		public DataTemplate FullWidthTemplate { get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			var property = item as PropertyViewModel;
			if (property != null)
			{
				if (property.IsFullWidth)
				{
					return FullWidthTemplate;
				}
				else if (property is CompositePropertyViewModel)
				{
					return TwoColumnCompositeTemplate;
				}
				return TwoColumnTemplate;
			}

			return base.SelectTemplate(item, container);
		}
	}
}
