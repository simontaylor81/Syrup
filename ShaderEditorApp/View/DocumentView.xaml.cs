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
using ICSharpCode.AvalonEdit.Search;

namespace ShaderEditorApp.View
{
	/// <summary>
	/// Interaction logic for DocumentView.xaml
	/// </summary>
	/// This control is just a straight wrapper around the AvalonEdit TextEditor control.
	/// It exists to give us an easy place in code-behind to customise the TextEditor control itself.
	public partial class DocumentView : UserControl
	{
		public DocumentView()
		{
			InitializeComponent();

			// Enable Find box.
			SearchPanel.Install(textEditor);
		}
	}
}
