using System.Windows;
using System.Windows.Controls;
using ShaderEditorApp.ViewModel.Properties;

namespace ShaderEditorApp.View.Properties
{
	/// <summary>
	/// Interaction logic for BoolPropertyView.xaml
	/// </summary>
	public partial class BoolPropertyView : UserControl
	{
		public BoolPropertyView()
		{
			InitializeComponent();
		}
	}

	internal class BoolPropertyViewFactory : IPropertyViewFactory
	{
		public int Priority => 10;
		public bool SupportsProperty(PropertyViewModel property) => property is ScalarPropertyViewModel<bool>;
		public FrameworkElement CreateView(PropertyViewModel property) => new BoolPropertyView();
	}
}
