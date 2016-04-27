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
using ShaderEditorApp.Interfaces;
using ShaderEditorApp.Model;
using ShaderEditorApp.Services;
using ShaderEditorApp.View;
using ShaderEditorApp.ViewModel;
using ShaderEditorApp.ViewModel.Workspace;
using Splat;
using SRPCommon.Logging;
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
			// Must create output window VM first as other stuff might need to log to it.
			var outputWindowVM = new OutputWindowViewModel();

			// Create a logger factory for logging to the output window and console.
			var loggerFactory = new CompositeLoggerFactory(outputWindowVM, new ConsoleLoggerFactory());

			// Register user settings provider.
			Locator.CurrentMutable.RegisterConstant(new UserSettings(loggerFactory), typeof(IUserSettings));

			// Initialise D3D device.
			_renderDevice = new RenderDevice();

			// Create workspace and corresponding view model.
			_workspace = new Workspace(_renderDevice, loggerFactory);
			_workspaceViewModel = new WorkspaceViewModel(_workspace, outputWindowVM, loggerFactory);

			// Close ourselves when the Exit command is triggered.
			_workspaceViewModel.Exit.Subscribe(_ => Close());

			// Create render window and assign it to its host.
			renderWindow = new RenderWindow(_renderDevice, _workspaceViewModel);
			viewportFrame.SetRenderWindow(renderWindow);

			DataContext = _workspaceViewModel;
			viewportFrame.DataContext = renderWindow.ViewportViewModel;

			CompositionTarget.Rendering += CompositionTarget_Rendering;

			// Hook up key bindings (can't be data-bound unfortunately).
			foreach (var cmd in _workspaceViewModel.KeyBoundCommands)
			{
				InputBindings.Add(new KeyBinding(cmd.Command, cmd.KeyGesture));
			}

			InitHighlighting();

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

		// Set up syntax highlighting for the editor control.
		private void InitHighlighting()
		{
			LoadSyntaxHighlightingDefinition("Python");
			LoadSyntaxHighlightingDefinition("HLSL");

			// Add C# highlighting definitions for .csx files.
			HighlightingManager.Instance.RegisterHighlighting(null, new[] { ".csx" },
				HighlightingManager.Instance.GetDefinition("C#"));
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
	}
}
