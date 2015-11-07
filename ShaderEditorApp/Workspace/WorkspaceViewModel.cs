﻿using System;
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
using ShaderEditorApp.ViewModel.Scene;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;

namespace ShaderEditorApp.Workspace
{
	// ViewModel for the application workspace, containing documents, docking windows, etc.
	public class WorkspaceViewModel : ReactiveObject, IWorkspace
	{
		public WorkspaceViewModel(RenderWindow renderWindow)
		{
			this.renderWindow = renderWindow;

			// Create documents list, and wrap in a read-only wrapper.
			documents = new ObservableCollection<DocumentViewModel>();
			Documents = new ReadOnlyObservableCollection<DocumentViewModel>(documents);

			// Create menu bar
			MenuBar = new MenuBarViewModel(this);

			// Create classes that handle scripting.
			scripting = new Scripting(this);
			scriptRenderControl = new ScriptRenderControl(this, renderWindow.Device, scripting);
			scripting.RenderInterface = scriptRenderControl.ScriptInterface;
			scriptRenderControl.RedrawRequired.Subscribe(_ => RedrawViewports());

			renderWindow.ScriptControl = scriptRenderControl;
			renderWindow.ViewportViewModel = ViewportViewModel;

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

		// TODO: Fix async void nastiness.
		private async void RunActiveScript()
		{
			// Save all so we running the latest contents.
			if (!SaveAllDirty())
			{
				// Save failed (likely Save As cancelled). Abort.
				return;
			}

			if (ActiveDocument != null && ActiveDocument.IsScript)
			{
				RunScriptFile(ActiveDocument.FilePath);
			}
			else if (_lastRunScript != null)
			{
				await RunScript(_lastRunScript);
			}
		}

		// TODO: Fix async void nastiness.
		internal async void RunScriptFile(string path)
		{
			var script = _scripts.GetOrAdd(path, () => new Script(path));
			await RunScript(script);
		}

		private async Task RunScript(Script script)
		{
			_lastRunScript = script;

			// Asynchronously execute the script.
			await scripting.RunScript(script);

			RedrawViewports();
		}

		private bool IsActiveScript() => ActiveDocument != null;

		// Do we have a scene loaded currently?
		public bool HasCurrentScene => currentScene != null;

		// Load the scene with the given filename and set it as the current one.
		public void SetCurrentScene(string path)
		{
			// Attempt to load the scene.
			var newScene = Scene.LoadFromFile(path);
			if (newScene != null)
			{
				currentScene = newScene;
				scriptRenderControl.Scene = currentScene;

				// Unsubscribe from previous scene.
				if (sceneChangeSubscription != null)
				{
					sceneChangeSubscription.Dispose();
				}

				// Redraw the scene when the scene changes.
				sceneChangeSubscription = currentScene.OnChanged.Subscribe(_ => RedrawViewports());

				SceneViewModel = new SceneViewModel(newScene);
			}
		}

		public void RedrawViewports()
		{
			renderWindow.Invalidate();
		}

		public string FindProjectFile(string name)
		{
			var shaderFileItem = Project.AllItems.FirstOrDefault(item => item.Name == name);
			return shaderFileItem != null ? shaderFileItem.AbsolutePath : null;
		}

		// Given an absolute or project-relative path, get an absolute path.
		public string GetAbsolutePath(string path)
		{
			if (Path.IsPathRooted(path))
			{
				return path;
			}
			return Path.Combine(project_.BasePath, path);
		}

		private ObservableCollection<DocumentViewModel> documents;
		public ReadOnlyObservableCollection<DocumentViewModel> Documents { get; }

		// Property that tracks the currently active document.
		private DocumentViewModel activeDocument;
		public DocumentViewModel ActiveDocument
		{
			get { return activeDocument; }
			//set { this.RaiseAndSetIfChanged(ref activeDocument, value); }
			set
			{
				if (value != activeDocument)
				{
					activeDocument = value;
					this.RaisePropertyChanged();

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
					this.RaisePropertyChanged();

					// If this is a document, update the active document property too.
					if (value is DocumentViewModel)
						ActiveDocument = (DocumentViewModel)value;

					// Update property source window too.
					// Don't change property source if when focusing on the properties window, otherwise you can't edit anything!
					if (!(value is View.PropertiesWindow))
					{
						// Use data context of focused window if it's a property source.
						var frameworkElement = value as FrameworkElement;
						if (frameworkElement != null)
						{
							FocusPropertySource = frameworkElement.DataContext as IPropertySource;
						}
						else
						{
							FocusPropertySource = null;
						}
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
					this.RaisePropertyChanged();
					this.RaisePropertyChanged("Properties");

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

		private SceneViewModel _sceneViewModel;
		public SceneViewModel SceneViewModel
		{
			get { return _sceneViewModel; }
			set
			{
				if (value != _sceneViewModel)
				{
					_sceneViewModel = value;
					this.RaisePropertyChanged();
					this.RaisePropertyChanged("Properties");

					// When the project's exposed properties change, so might ours.
					// TODO: Rx-ify?
					_sceneViewModel.PropertyChanged += (o, e) =>
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

		public MenuBarViewModel MenuBar { get; }

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

			this.RaisePropertyChanged("Properties");
		}

		// Save all dirty documents.
		private bool SaveAllDirty()
		{
			var result = true;

			foreach (var document in Documents)
			{
				if (document.IsDirty)
				{
					result = document.Save() & result;
				}
			}

			return result;
		}

		// Viewport view model that contains settings for the viewport (e.g. camera mode).
		private ViewportViewModel viewportViewModel = new ViewportViewModel();
		public ViewportViewModel ViewportViewModel => viewportViewModel;

		// Rendering/script related stuff.
		private ScriptRenderControl scriptRenderControl;
		private Scripting scripting;
		private RenderWindow renderWindow;

		private Scene currentScene;
		private IDisposable sceneChangeSubscription;

		// Previously run scripts.
		private readonly Dictionary<string, Script> _scripts = new Dictionary<string, Script>();

		// Script file that was last run.
		private Script _lastRunScript;

		// TODO: Make observable?
		public bool RealTimeMode
		{
			get { return renderWindow != null && renderWindow.RealTimeMode; }
			set { renderWindow.RealTimeMode = value; }
		}

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
		public ICommand NewProjectCmd => RelayCommand.LazyInit(ref newProjectCmd, param => NewProject());

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
		public ICommand NewDocumentCmd => RelayCommand.LazyInit(ref newDocumentCmd, param => NewDocument());

		// Command to close the currently active document.
		private NamedCommand closeActiveDocumentCmd;
		public INamedCommand CloseActiveDocumentCmd
			=> NamedCommand.LazyInit(ref closeActiveDocumentCmd, "Close",
				param => CloseDocument(ActiveDocument), param => ActiveDocument != null);

		// Command to save the currently active document.
		private NamedCommand saveActiveDocumentCmd;
		public INamedCommand SaveActiveDocumentCmd
			=> NamedCommand.LazyInit(ref saveActiveDocumentCmd, "Save",
				param => ActiveDocument.Save(), param => ActiveDocument != null);

		// Command to save the currently active document under a new filename.
		private NamedCommand saveActiveDocumentAsCmd;
		public INamedCommand SaveActiveDocumentAsCmd
			=> NamedCommand.LazyInit(ref saveActiveDocumentAsCmd, "Save As",
				param => ActiveDocument.SaveAs(), param => ActiveDocument != null);

		// Command to execute the currently active script document.
		private NamedCommand runActiveScriptCmd;
		public INamedCommand RunActiveScriptCmd
			=> NamedCommand.LazyInit(ref runActiveScriptCmd, "Run Current Script",
				param => RunActiveScript(),
				param => IsActiveScript());

		// Command to save all (dirty) open documents.
		private NamedCommand saveAllCmd;
		public INamedCommand SaveAllCmd
			=> NamedCommand.LazyInit(ref saveAllCmd, "Save All",
				param => SaveAllDirty());

		#endregion
	}
}
