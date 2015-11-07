using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ShaderEditorApp.ViewModel;

namespace ShaderEditorApp
{
	class PaneStyleSelector : StyleSelector
	{
		// Style to use for the various anchorable tool windows.
		public Style ToolStyle { get; set; }

		// Style to use for the document tabs.
		public Style DocumentStyle { get; set; }

		public override Style SelectStyle(object item, DependencyObject container)
		{
			if (item is DocumentViewModel)
				return DocumentStyle;
			else
				return ToolStyle;
		}
	}

	class PaneTemplateSelector : DataTemplateSelector
	{
		// Template to use for the document tabs.
		public DataTemplate DocumentTemplate { get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			if (item is DocumentViewModel)
				return DocumentTemplate;

			return base.SelectTemplate(item, container);
		}
	}
}
