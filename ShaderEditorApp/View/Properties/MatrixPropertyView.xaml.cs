using System.Windows;
using System.Windows.Controls;
using ShaderEditorApp.ViewModel.Properties;

namespace ShaderEditorApp.View.Properties
{
	/// <summary>
	/// Interaction logic for MatrixPropertyView.xaml
	/// </summary>
	public partial class MatrixPropertyView : UserControl
	{
		public MatrixPropertyView()
		{
			InitializeComponent();
		}
	}

	internal class MatrixPropertyViewFactory : IPropertyViewFactory
	{
		public int Priority => 20;
		public bool SupportsProperty(PropertyViewModel property) => property is MatrixPropertyViewModel;
		public FrameworkElement CreateView(PropertyViewModel property) => new MatrixPropertyView();
	}
}
