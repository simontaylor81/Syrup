using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ReactiveUI;
using ShaderEditorApp.Model;
using ShaderEditorApp.Services;
using ShaderEditorApp.View;
using ShaderEditorApp.ViewModel;
using SRPCommon.Util;
using SRPRendering;

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

		private Workspace _workspace;
		private WorkspaceViewModel _workspaceViewModel;
		private RenderWindow renderWindow;
		private RenderDevice _renderDevice;

		private bool isCloseAllowed = false;

		private void Window_Initialized(object sender, EventArgs e)
		{
			// Initialise D3D device.
			_renderDevice = new RenderDevice();

			// Create workspace and corresponding view model.
			_workspace = new Workspace(_renderDevice);
			_workspaceViewModel = new WorkspaceViewModel(_workspace);

			// Close ourselves when the Exit command is triggered.
			_workspaceViewModel.Exit.Subscribe(_ => Close());

			// Create render window and assign it to its host.
			renderWindow = new RenderWindow(_renderDevice, _workspaceViewModel);
			viewportFrame.SetRenderWindow(renderWindow);

			OutputLogger.Instance.AddTarget(outputWindow);

			DataContext = _workspaceViewModel;
			viewportFrame.DataContext = renderWindow.ViewportViewModel;

			CompositionTarget.Rendering += CompositionTarget_Rendering;

			// Hook up key bindings (can't be data-bound unfortunately).
			foreach (var cmd in _workspaceViewModel.KeyBoundCommands)
			{
				InputBindings.Add(new KeyBinding(cmd.Command, cmd.KeyGesture));
			}

			// Set up syntax highlighting for the editor control.
			LoadSyntaxHighlightingDefinition("Python");
			LoadSyntaxHighlightingDefinition("HLSL");

			// Load a file specified on the commandline.
			var commandlineParams = Environment.GetCommandLineArgs();
			if (commandlineParams.Length > 1)
			{
				var filename = commandlineParams[1];
				if (File.Exists(filename))
				{
					if (string.Equals(Path.GetExtension(filename), ".srpproj", StringComparison.InvariantCultureIgnoreCase))
					{
						// Open .srpproj files as projects.
						_workspace.OpenProject(filename);
					}
					else
					{
						// Open other files as documents.
						_workspaceViewModel.OpenDocumentSet.OpenDocument(filename, false);
					}
				}
			}
		}

		// Notification that the window is about to closed, allowing cancellation.
		private async void Window_Closing(object sender, CancelEventArgs e)
		{
			// If closing has already been allowed, do it.
			if (isCloseAllowed)
			{
				return;
			}

			// Notify the view model that we're closing, and ask if we're allowed.
			var task = _workspaceViewModel.OnExit();

			// If the task completed immediatly (no actual async stuff required),
			// then just use the standard cancel mechanism.
			if (task.IsCompleted)
			{
				e.Cancel = !task.Result;
			}
			else
			{
				// This event isn't really async, so always cancel, then re-fire closed if-and-when confirmed.
				e.Cancel = true;

				if (await task)
				{
					// Closing is now allowed. Mark it as such and re-trigger closing process.
					isCloseAllowed = true;
					Close();
				}
			}
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			_workspace.Dispose();
			_renderDevice.Dispose();
		}

		void CompositionTarget_Rendering(object sender, EventArgs e)
		{
			renderWindow.Tick();
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
		}
	}
}
