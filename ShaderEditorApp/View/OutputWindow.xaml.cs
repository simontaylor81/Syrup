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
using ShaderEditorApp.ViewModel;
using SRPCommon.Logging;
using SRPCommon.Util;

namespace ShaderEditorApp.View
{
	/// <summary>
	/// Interaction logic for OutputWindow.xaml
	/// </summary>
	public partial class OutputWindow : UserControl
	{
		public OutputWindow()
		{
			InitializeComponent();
		}

		private void LogTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var textbox = sender as TextBox;
			var viewmodel = DataContext as OutputWindowViewModel;
			if (e.ChangedButton == MouseButton.Left && textbox != null && viewmodel != null)
			{
				var line = textbox.GetLineText(textbox.GetLineIndexFromCharacterIndex(textbox.CaretIndex));
				viewmodel.Goto(line);
			}
		}
	}
}
