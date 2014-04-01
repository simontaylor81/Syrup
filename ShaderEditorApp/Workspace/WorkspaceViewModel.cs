using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ShaderEditorApp.MVVMUtil;
using ShaderEditorApp.Projects;
using ShaderEditorApp.View;
using ShaderEditorApp.ViewModel;
using SRPCommon.Interfaces;
using SRPCommon.Scene;
using SRPCommon.Scripting;
using SRPCommon.UserProperties;
using SRPCommon.Util;
using SRPRendering;
using ShaderEditorApp.ViewModel.Projects;

namespace ShaderEditorApp.Workspace
{
	// ViewModel for the application workspace, containing documents, docking windows, etc.
	public class WorkspaceViewModel : ViewModelBase, IWorkspace
	{
		public WorkspaceViewModel(RenderWindow renderWindow)
		{
			this.renderWindow = renderWindow;

			// Create documents list, and wrap in a read-only wrapper.
			documents = new ObservableCollection<DocumentViewModel>();
			Documents = new ReadOnlyObservableCollection<DocumentViewModel>(documents);

			// Create classes that handle scripting.
			scripting = new Scripting();
			scriptRenderControl = new ScriptRenderControl(this, renderWindow.Device, scripting);
			scripting.RenderInterface = scriptRenderControl.ScriptInterface;

			renderWindow.ScriptControl = scriptRenderControl;
			renderWindow.ViewportViewModel = ViewportViewModel;

			ViewportsDirtied += () => renderWindow.Invalidate();

			Application.Current.Activated += (o, _e) => isAppForeground = true;
			Application.Current.Deactivated += (o, _e) => isAppForeground = false;


			// Load a file specified on the commandline.
			var commandlineParams = Environment.GetCommandLineArgs();
			if (commandlineParams.Length > 1)
			{
				var filename = commandlineParams[1];
				if (File.Exists(filename))
				{
					if (String.Equals(Path.GetExtension(filename), ".srpproj", StringComparison.InvariantCultureIgnoreCase))
					{
						// Open .srpproj files as projects.
						OpenProject(filename);
					}
					else
					{
						// Open other files as documents.
						OpenDocument(filename, false);
					}
				}
			}

			// Recreate property list when the script's properties change.
			scriptRenderControl.Properties.CollectionChanged += (o, e) => RecreatePropertiesList();
		}

		public void Tick()
		{
			CheckModifiedDocuments();
		}

		// Open the project with the given path.
		private void OpenProject(string path)
		{
			Project = Project.LoadFromFile(path);

			// Reload saved open documents.
			foreach (var file in Project.SavedOpenDocuments)
				OpenDocument(file, false);
		}

		// Open a project by asking the user for a project file to open.
		private void OpenProjectPrompt()
		{
			var dialog = new OpenFileDialog();
			dialog.Filter = "SRP Projects|*.srpproj";

			// Put initial directory in the same place as the current project, if there is one.
			if (Project != null)
				dialog.InitialDirectory = Project.BasePath;

			var result = dialog.ShowDialog();
			if (result == true)
			{
				OpenProject(dialog.FileName);
			}
		}

		// Create a new project.
		private void NewProject()
		{
			// Don't just create a project without a backing file, prompt the user to create one.
			var dialog = new SaveFileDialog();
			dialog.Filter = "SRP Projects|*.srpproj";

			// Put initial directory in the same place as the current project, if there is one.
			if (Project != null)
				dialog.InitialDirectory = Project.BasePath;

			var result = dialog.ShowDialog();
			if (result == true)
			{
				// Create a new project, and immediately save it to the given file.
				Project = Project.CreateNew(dialog.FileName);
			}
		}

		internal void OpenDocument(string path, bool bReload)
		{
			// Look for an already open document.
			var document = documents.FirstOrDefault(doc => doc.FilePath == path);
			if (document != null)
			{
				// Force reload if required.
				if (bReload)
					document.LoadContents();
			}
			else if (File.Exists(path))
			{
				// Create a new document.
				document = new DocumentViewModel(this, path);
				documents.Add(document);
			}
			else
			{
				OutputLogger.Instance.LogLine(LogCategory.Log, "File not found: " + path);
			}

			// Make active document.
			ActiveDocument = document;
		}

		// Open a document by asking the user for a file to open.
		private void OpenDocumentPrompt()
		{
			var dialog = new OpenFileDialog();
			dialog.Filter = "All files|*.*";

			// Put initial directory in the same place as the current document, if there is one.
			if (ActiveDocument != null && !String.IsNullOrEmpty(ActiveDocument.FilePath))
				dialog.InitialDirectory = Path.GetDirectoryName(ActiveDocument.FilePath);
			else if (Project != null)
				dialog.InitialDirectory = Project.BasePath;

			var result = dialog.ShowDialog();
			if (result == true)
			{
				// Force a reload if the document is already loaded.
				OpenDocument(dialog.FileName, true);
			}
		}

		// Create a new document.
		private void NewDocument()
		{
			var document = new DocumentViewModel(this);
			documents.Add(document);
			ActiveDocument = document;
		}

		internal void CloseDocument(DocumentViewModel document)
		{
			document.Dispose();
			documents.Remove(document);

			if (ActiveDocument == document)
			{
				// TODO: Better new active document logic.
				ActiveDocument = documents.FirstOrDefault();
			}
		}

		// Notification that a document has been modified, and might need to be reloaded. Thread-safe.
		internal void AddModifiedDocument(DocumentViewModel document)
		{
			lock (modifiedDocuments)
			{
				modifiedDocuments.Add(document);
			}
		}

		// If there are any externally-modified files, prompt to reload them.
		private void CheckModifiedDocuments()
		{
			// Don't do anything if the app's not in the foreground.
			if (!isAppForeground)
				return;

			DocumentViewModel[] docsToReload;
			lock (modifiedDocuments)
			{
				docsToReload = modifiedDocuments.ToArray();
				modifiedDocuments.Clear();
			}

			foreach (var document in docsToReload)
			{
				// Prompt to reload.
				var result = MessageBox.Show(
					string.Format("{0} was modified by an external program. Would you like to reload it?", Path.GetFileName(document.FilePath)),
					"SRP", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
				{
					document.LoadContents();
				}
			}
		}

		private async void RunActiveScript()
		{
			if (ActiveDocument != null)
			{
				// Asynchronously execute the script.
				await scripting.RunScript(ActiveDocument.Contents);

				RedrawViewports();
			}
		}

		internal async void RunScriptFile(string path)
		{
			// Asynchronously execute the script.
			await scripting.RunScriptFromFile(path);

			RedrawViewports();
		}

		private bool IsActiveScript()
		{
			return ActiveDocument != null;
		}

		// Load the scene with the given filename and set it as the current one.
		public void SetCurrentScene(string path)
		{
			// Attempt to load the scene.
			var newScene = Scene.LoadFromFile(path);
			if (newScene != null)
			{
				currentScene = newScene;
				scriptRenderControl.Scene = currentScene;
			}
		}

		public void RedrawViewports()
		{
			if (!scriptRenderControl.IgnoreRedrawRequests)
			{
				ViewportsDirtied();
			}
		}

		public string FindProjectFile(string name)
		{
			var shaderFileItem = Project.AllItems.FirstOrDefault(item => item.Name == name);
			return shaderFileItem != null ? shaderFileItem.AbsolutePath : null;
		}

		private ObservableCollection<DocumentViewModel> documents;
		public ReadOnlyObservableCollection<DocumentViewModel> Documents { get; private set; }

		// Property that tracks the currently active document.
		private DocumentViewModel activeDocument;
		public DocumentViewModel ActiveDocument
		{
			get { return activeDocument; }
			set
			{
				if (value != activeDocument)
				{
					activeDocument = value;
					OnPropertyChanged();

					// Update the active window too so the UI is updated.
					ActiveWindow = value;
				}
			}
		}

		// Property that tracks the active window, document or otherwise.
		private object activeWindow;
		public object ActiveWindow
		{
			get { return activeWindow; }
			set
			{
				if (value != activeWindow)
				{
					activeWindow = value;
					OnPropertyChanged();

					// If this is a document, update the active document property too.
					if (value is DocumentViewModel)
						ActiveDocument = (DocumentViewModel)value;

					// Update property source window too.
					// Don't use property windows as the properties source, otherwise you can't edit anything!
					// TODO: Make this more generic?
					if (!(value is View.PropertiesWindow))
					{
						if (value is ProjectBrowser)
							FocusPropertySource = ProjectViewModel;
						else
							FocusPropertySource = null;
					}
				}
			}
		}

		private Project project_;
		public Project Project
		{
			get { return project_; }
			private set
			{
				// Close existing documents when opening a new project.
				// TODO: Prompt to save.
				DisposableUtil.DisposeList(documents);

				project_ = value;
				ProjectViewModel = new ProjectViewModel(project_, this);

				// Load default scene, if present.
				if (Project.DefaultScene != null)
				{
					SetCurrentScene(Project.DefaultScene.AbsolutePath);
				}

				// Run startup scripts.
				foreach (var script in value.StartupScripts)
					RunScriptFile(script);
			}
		}

		private ProjectViewModel projectViewModel;
		public ProjectViewModel ProjectViewModel
		{
			get { return projectViewModel; }
			set
			{
				if (value != projectViewModel)
				{
					projectViewModel = value;
					OnPropertyChanged();
					OnPropertyChanged("Properties");

					// When the project's exposed properties change, so might ours.
					projectViewModel.PropertyChanged += (o, e) =>
						{
							if (e.PropertyName == "Properties")
							{
								RecreatePropertiesList();
							}
						};
				}
			}
		}

		// Property source to use based on focus.
		private IPropertySource focusPropertySource;
		public IPropertySource FocusPropertySource
		{
			get { return focusPropertySource; }
			set
			{
				if (value != focusPropertySource)
				{
					focusPropertySource = value;
					RecreatePropertiesList();
				}
			}
		}

		public IEnumerable<PropertyViewModel> Properties { get; private set; }

		private void RecreatePropertiesList()
		{
			// Use focussed window if it's a property source, otherwise
			// fallback on the render properties (i.e. shader and user variables).
			var source = FocusPropertySource != null ? FocusPropertySource.Properties : scriptRenderControl.Properties;

			// Create viewmodels for each property.
			Properties = source
				.EmptyIfNull()
				.Select(prop => PropertyViewModelFactory.CreateViewModel(prop))
				.ToArray();

			OnPropertyChanged("Properties");
		}

		// Viewport view model that contains settings for the viewport (e.g. camera mode).
		private ViewportViewModel viewportViewModel = new ViewportViewModel();
		public ViewportViewModel ViewportViewModel { get { return viewportViewModel; } }

		// Rendering/script related stuff.
		private ScriptRenderControl scriptRenderControl;
		private Scripting scripting;
		private RenderWindow renderWindow;

		private Scene currentScene;

		// TODO: Multiple viewports.
		// TODO: Move info needed?
		// TODO: Cleanup, consoliate. Interface?
		public System.Drawing.Size ViewportSize { get { return renderWindow.Size; } }

		// Event to fire when render windows need to be updated.
		private event Action ViewportsDirtied;

		// List of documents that have been externally modified.
		private HashSet<DocumentViewModel> modifiedDocuments = new HashSet<DocumentViewModel>();

		// Is the application in the foreground?
		private bool isAppForeground = false;

		// Commands that we expose to the view.
		#region Commands

		// Command to open a project (given a file path prompts the user unless path given).
		private RelayCommand openProjectCmd;
		public ICommand OpenProjectCmd
		{
			get
			{
				return RelayCommand.LazyInit(ref openProjectCmd, param =>
					{
						if (param is string)
							OpenProject((string)param);
						else
							OpenProjectPrompt();
					});
			}
		}

		// Command to create a new project, closing the old one.
		private RelayCommand newProjectCmd;
		public ICommand NewProjectCmd { get { return RelayCommand.LazyInit(ref newProjectCmd, param => NewProject()); } }

		// Command to open a document (shows the open file dialog unless passed a string).
		private RelayCommand openDocumentCmd;
		public ICommand OpenDocumentCmd
		{
			get
			{
				return RelayCommand.LazyInit(ref openDocumentCmd, param =>
					{
						if (param is string)
							OpenDocument((string)param, false);
						else
							OpenDocumentPrompt();
					});
			}
		}

		// Command to create a new document.
		private RelayCommand newDocumentCmd;
		public ICommand NewDocumentCmd { get { return RelayCommand.LazyInit(ref newDocumentCmd, param => NewDocument()); } }

		// Command to close the currently active document.
		private NamedCommand closeActiveDocumentCmd;
		public NamedCommand CloseActiveDocumentCmd
		{
			get
			{
				return NamedCommand.LazyInit(ref closeActiveDocumentCmd, "Close",
					param => CloseDocument(ActiveDocument), param => ActiveDocument != null);
			}
		}

		// Command to save the currently active document.
		private NamedCommand saveActiveDocumentCmd;
		public NamedCommand SaveActiveDocumentCmd
		{
			get
			{
				return NamedCommand.LazyInit(ref saveActiveDocumentCmd, "Save",
					param => ActiveDocument.Save(), param => ActiveDocument != null);
			}
		}

		// Command to save the currently active document under a new filename.
		private NamedCommand saveActiveDocumentAsCmd;
		public NamedCommand SaveActiveDocumentAsCmd
		{
			get
			{
				return NamedCommand.LazyInit(ref saveActiveDocumentAsCmd, "Save As",
					param => ActiveDocument.SaveAs(), param => ActiveDocument != null);
			}
		}

		// Command to execute the currently active script document.
		private NamedCommand runActiveScriptCmd;
		public NamedCommand RunActiveScriptCmd
		{
			get
			{
				return NamedCommand.LazyInit(ref runActiveScriptCmd, "Run Current Script",
					param => RunActiveScript(),
					param => IsActiveScript());
			}
		}

		#endregion
	}
}
