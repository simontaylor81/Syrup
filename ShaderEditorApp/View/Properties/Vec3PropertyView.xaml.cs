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
using ShaderEditorApp.ViewModel.Properties;

namespace ShaderEditorApp.View.Properties
{
	/// <summary>
	/// Interaction logic for Vec3PropertyView.xaml
	/// </summary>
	public partial class Vec3PropertyView : UserControl
	{
		public Vec3PropertyView()
		{
			InitializeComponent();
		}
	}

	internal class Vec3PropertyViewFactory : IPropertyViewFactory
	{
		public int Priority => 10;
		public bool IsFullWidth => true;

		public bool SupportsProperty(PropertyViewModel property) => property is Vec3PropertyViewModel;
		//{
		//	// We support 3 component float vectors.
		//	var vectorProperty = property as VectorPropertyViewModel;
		//	return vectorProperty != null && vectorProperty.SubProperties.Length == 3
		//		&& vectorProperty.SubProperties.All(component => component is ScalarPropertyViewModel<float>);
		//}

		public FrameworkElement CreateView(PropertyViewModel property) => new Vec3PropertyView();
	}
}
