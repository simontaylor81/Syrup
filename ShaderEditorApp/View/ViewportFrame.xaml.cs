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

namespace ShaderEditorApp.View
{
	/// <summary>
	/// Interaction logic for ViewportFrame.xaml
	/// </summary>
	public partial class ViewportFrame : UserControl
	{
		public ViewportFrame()
		{
			InitializeComponent();
		}

		// The underlying RenderWindow object.
		public void SetRenderWindow(RenderWindow value)
		{
			renderWindowHost.Child = value;
		}
	}
}
