using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ShaderEditorApp.MVVMUtil
{
	// Converter that converts a boolean value to a font weight, where true is bold, and false is normal.
	public class BoolToBoldConverter : IValueConverter
	{
		public object Convert(object value, Type targetType,
							  object parameter, CultureInfo culture)
		{
			return ((bool)value) ? FontWeights.Bold : FontWeights.Normal;
		}

		public object ConvertBack(object value, Type targetType,
								  object parameter, CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}
}
