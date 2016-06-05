using System.Windows;
using System.Windows.Controls;
using ShaderEditorApp.ViewModel.Properties;

namespace ShaderEditorApp.View.Properties
{
	/// <summary>
	/// Interaction logic for ChoicePropertyView.xaml
	/// </summary>
	public partial class ChoicePropertyView : UserControl
	{
		public ChoicePropertyView()
		{
			InitializeComponent();
		}
	}

	internal class ChoicePropertyViewFactory : IPropertyViewFactory
	{
		public int Priority => 10;
		public bool SupportsProperty(PropertyViewModel property) => property is ChoicePropertyViewModel;
		public FrameworkElement CreateView(PropertyViewModel property) => new ChoicePropertyView();
	}
}
