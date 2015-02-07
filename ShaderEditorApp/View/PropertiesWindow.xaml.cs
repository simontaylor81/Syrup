﻿using System;
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
		public DataTemplate CompositeTemplate { get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			// TODO: Can this be handled more generically?
			if (item is ShaderEditorApp.SampleData.DummyPropertyFloat)
			{
				return ScalarTemplate;
			}
			else if (item is ShaderEditorApp.SampleData.DummyPropertyBool)
			{
				return BoolTemplate;
			}
			else if (item is ShaderEditorApp.SampleData.DummyCompositeProperty)
			{
				return CompositeTemplate;
			}

			// Anything that just wants a single text box can use the scalar template.
			if (item is ScalarPropertyViewModel<float> || item is ScalarPropertyViewModel<string>)
				return ScalarTemplate;
			else if (item is ScalarPropertyViewModel<bool>)
				return BoolTemplate;
			else if (item is VectorPropertyViewModel)
				return CompositeTemplate;

			return base.SelectTemplate(item, container);
		}
	}
}
