using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

using ShaderEditorApp.Workspace;
using ShaderEditorApp.View;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using SRPCommon.Util;

namespace ShaderEditorApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private WorkspaceViewModel workspace;
		private RenderWindow renderWindow;

		private void Window_Initialized_1(object sender, EventArgs e)
		{
			// Create render window and assign it to its host.
			renderWindow = new RenderWindow();
			viewportFrame.SetRenderWindow(renderWindow);

			OutputLogger.Instance.AddTarget(outputWindow);

			workspace = new WorkspaceViewModel(renderWindow);
			DataContext = workspace;

			viewportFrame.DataContext = workspace.ViewportViewModel;

			CompositionTarget.Rendering += CompositionTarget_Rendering;

			// Set up syntax highlighting for the editor control.
			LoadSyntaxHighlightingDefinition("Python");
			LoadSyntaxHighlightingDefinition("HLSL");
		}

		void CompositionTarget_Rendering(object sender, EventArgs e)
		{
			renderWindow.Tick();
			workspace.Tick();
		}

		private void LoadSyntaxHighlightingDefinition(string language)
		{
			string appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			string filename = Path.Combine(appPath, language + ".xshd");

			// Load definition file to extract the extensions to use.
			var xdoc = System.Xml.Linq.XDocument.Load(filename);
			var extensions = xdoc.Root.Attribute("extensions").Value.Split(';');

			// Load again to read the full syntax definition file.
			using (var reader = System.Xml.XmlReader.Create(filename))
			{
				var defintion = HighlightingLoader.Load(reader, HighlightingManager.Instance);
				HighlightingManager.Instance.RegisterHighlighting(language, extensions, defintion);
			}
		}

		// Shim to allow customising the TextEditor object for a document that we can't do in XAML.
		private void TextEditor_Initialized(object sender, EventArgs e)
		{
			var textEditor = (TextEditor)sender;
			var documentViewModel = (DocumentViewModel)textEditor.DataContext;

			// Set up the TextArea (can't set properties of sub-objects in XAML).
			textEditor.TextArea.SelectionCornerRadius = 0.0;

			// Set the caret, selection and scroll position to those stored in the viewmodel.
			textEditor.ScrollToHorizontalOffset(documentViewModel.HorizontalScrollPosition);
			textEditor.ScrollToVerticalOffset(documentViewModel.VerticalScrollPosition);

			textEditor.SelectionStart = documentViewModel.SelectionStart;
			textEditor.SelectionLength = documentViewModel.SelectionLength;

			// Set caret last, as setting the selection modifies the caret too.
			textEditor.TextArea.Caret.Offset = documentViewModel.CaretPosition;

			// Hook up events to notify the VM when the selection/caret/scroll position change.
			// This really should be data bindings, but AvalonEdit doesn't expose the right dependency properties.
			// Note: We don't need to unsubscribe these event handlers because the view model and the view have the same lifetime.
			textEditor.TextArea.Caret.PositionChanged += (o, _e) =>
				{
					documentViewModel.CaretPosition = textEditor.TextArea.Caret.Offset;
				};
			textEditor.TextArea.TextView.ScrollOffsetChanged += (_o, _e) =>
				{
					documentViewModel.HorizontalScrollPosition = textEditor.TextArea.TextView.HorizontalOffset;
					documentViewModel.VerticalScrollPosition = textEditor.TextArea.TextView.VerticalOffset;
				};
			textEditor.TextArea.SelectionChanged += (_o, _e) =>
				{
					documentViewModel.SelectionStart = textEditor.SelectionStart;
					documentViewModel.SelectionLength = textEditor.SelectionLength;
				};

			// Set syntax highlighting definition to use.
			textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(
				System.IO.Path.GetExtension(documentViewModel.FilePath));
		}
	}
}
