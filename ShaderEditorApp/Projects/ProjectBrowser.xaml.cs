using System;
using System.Collections.Generic;
using System.Globalization;
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
using ShaderEditorApp.ViewModel;

namespace ShaderEditorApp.Projects
{
	/// <summary>
	/// Interaction logic for ProjectBrowser.xaml
	/// </summary>
	public partial class ProjectBrowser : UserControl
	{
		public ProjectBrowser()
		{
			InitializeComponent();
		}

		private void OnItemDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var node = (IHierarchicalBrowserNodeViewModel)projectTree.SelectedItem;
			if (node != null && node.DefaultCmd != null && node.DefaultCmd.CanExecute(null))
			{
				node.DefaultCmd.Execute(null);
			}
		}

		private void projectTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			// Update the view-model's active item when the selection changes.
			// Can't use a binding for this, annoyingly, as SelectedItem is read-only.
			var viewModel = (IHierarchicalBrowserRootViewModel)DataContext;
			viewModel.ActiveItem = (IHierarchicalBrowserNodeViewModel)projectTree.SelectedItem;
		}
	}
}
