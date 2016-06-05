using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using ShaderEditorApp.ViewModel.Properties;

namespace ShaderEditorApp.View.Properties
{
	// ContentControl for hosting the appropriate view for a user property.
	class UserPropertyHost : ContentControl
	{
		public UserPropertyHost()
		{
			DataContextChanged += OnDataContextChanged;
		}

		private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
		{
			// TEMP: Select manually
			if (DataContext is ScalarPropertyViewModel<float> || DataContext is ScalarPropertyViewModel<string> || DataContext is ScalarPropertyViewModel<int>)
			{
				Content = new SimpleScalarPropertyView();
			}
			else if (DataContext is ScalarPropertyViewModel<bool>)
			{
				Content = new BoolPropertyView();
			}
			else if (DataContext is ChoicePropertyViewModel)
			{
				Content = new ChoicePropertyView();
			}
			else if (DataContext is VectorPropertyViewModel)
			{
				Content = new VectorPropertyView();
			}
			else if (DataContext is MatrixPropertyViewModel)
			{
				Content = new MatrixPropertyView();
			}
		}
	}
}
