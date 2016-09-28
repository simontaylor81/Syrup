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
	// Selector for choosing the appropriate data template for user property display,
	// based on whether the property is full-width or just renders in the value column.
	class FullWidthPropertyTemplateSelector : DataTemplateSelector
	{
		public DataTemplate TwoColumnTemplate { get; set; }
		public DataTemplate FullWidthTemplate { get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			var property = item as PropertyViewModel;
			if (property != null)
			{
				return property.IsFullWidth ? FullWidthTemplate : TwoColumnTemplate;
			}

			return base.SelectTemplate(item, container);
		}
	}
}
