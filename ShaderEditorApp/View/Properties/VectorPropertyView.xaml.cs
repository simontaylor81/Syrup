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
	/// Interaction logic for VectorPropertyView.xaml
	/// </summary>
	public partial class VectorPropertyView : UserControl
	{
		public VectorPropertyView()
		{
			InitializeComponent();
		}
	}

	internal class VectorPropertyViewFactory : IPropertyViewFactory
	{
		public int Priority => 20;
		public bool IsFullWidth => false;
		public bool SupportsProperty(PropertyViewModel property) => property is VectorPropertyViewModel;
		public FrameworkElement CreateView(PropertyViewModel property) => new VectorPropertyView();
	}
}
