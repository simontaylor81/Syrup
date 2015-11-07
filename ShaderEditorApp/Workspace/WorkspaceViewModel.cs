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
using ShaderEditorApp.ViewModel.Scene;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using System.Reactive.Linq;
using ShaderEditorApp.Model;

namespace ShaderEditorApp.ViewModel
{
	// ViewModel for the application workspace, containing documents, docking windows, etc.
	public class WorkspaceViewModel : ReactiveObject
	{
		public WorkspaceViewModel(Model.Workspace _workspace, RenderWindow renderWindow)
		{
			this.Workspace = _workspace;
			this._renderWindow = renderWindow;

			// Create documents list, and wrap in a read-only wrapper.
			documents = new ObservableCollection<DocumentViewModel>();
			Documents = new ReadOnlyObservableCollection<DocumentViewModel>(documents);

			// Create menu bar
			MenuBar = new MenuBarViewModel(this);

			renderWindow.ViewportViewModel = ViewportViewModel;

			{
				// Active document is just the most recent active window that was a document.
				_activeDocument = this.WhenAnyValue(x => x.ActiveWindow)
					.OfType<DocumentViewModel>()
					.ToProperty(this, x => x.ActiveDocument);
			}

			{
				// Get properties from active window if it's a property source.
				// Technically this doesn't need to be a property as it's not used by anything
				// other than the Properties source, but making it a property allows us to use
				// WhenAny, which simplifies everything greatly.
				_focusPropertySource = this.WhenAnyValue(x => x.ActiveWindow)
					.Where(window => !(window is PropertiesWindow))
					.Select(window => (window as FrameworkElement)?.DataContext as IPropertySource)
					.ToProperty(this, x => x.FocusPropertySource);
			}

			{
				// Combine sources of properties into a single observable.
				var propertySources = Observable.Merge(
					this.WhenAny(x => x.FocusPropertySource, x => x.FocusPropertySource.Properties, (x, y) => Unit.Default),
					_workspace.ScriptRenderControl.PropertiesChanged.Select(_ => Unit.Default));

				// Convert that into a stream of property view model lists.
				_properties = propertySources
					// Use focussed window if it's a property source, otherwise
					// fallback on the render properties (i.e. shader and user variables).
					.Select(x => FocusPropertySource != null ? FocusPropertySource.Properties : _workspace.ScriptRenderControl.Properties)
					.Select(x => x
						// Create viewmodels for each property.
						.EmptyIfNull()
						.Select(prop => PropertyViewModelFactory.CreateViewModel(prop))
						.ToArray())
					.ToProperty(this, x => x.Properties);
			}

			// When the project changes, re-open documents that were open last time.
			_workspace.WhenAnyValue(x => x.Project).Subscribe(project =>
			{
				if (project != null)
				{
					foreach (var file in project.SavedOpenDocuments)
					{
						OpenDocument(file, false);
					}
				}
				else
				{
					CloseAllDocuments();
				}
			});

			// Project view model tracks the underlying project.
			_projectViewModel = _workspace.WhenAnyValue(x => x.Project)
				.Select(project => new ProjectViewModel(project, this))
				.ToProperty(this, x => x.ProjectViewModel);

			// Scene view model tracks the underlying scene.
			_sceneViewModel = _workspace.WhenAnyValue(x => x.CurrentScene)
				.Select(scene => new SceneViewModel(scene))
				.ToProperty(this, x => x.SceneViewModel);
		}

		public void Tick()
		{
			CheckModifiedDocuments();
		}

		// Open a project by asking the user for a project file to open.
		private void OpenProjectPrompt()
		{
			var dialog = new OpenFileDialog();
			dialog.Filter = "SRP Projects|*.srpproj";

			// Put initial directory in the same place as the current project, if there is one.
			if (Workspace.Project != null)
			{
				dialog.InitialDirectory = Workspace.Project.BasePath;
			}

			var result = dialog.ShowDialog();
			if (result == true)
			{
				Workspace.OpenProject(dialog.FileName);
			}
		}

		// Create a new project.
		private void NewProject()
		{
			// Don't just create a project without a backing file, prompt the user to create one.
			var dialog = new SaveFileDialog();
			dialog.Filter = "SRP Projects|*.srpproj";

			// Put initial directory in the same place as the current project, if there is one.
			if (Workspace.Project != null)
			{
				dialog.InitialDirectory = Workspace.Project.BasePath;
			}

			var result = dialog.ShowDialog();
			if (result == true)
			{
				Workspace.NewProject(dialog.FileName);
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
			ActiveWindow = document;
		}

		// Open a document by asking the user for a file to open.
		private void OpenDocumentPrompt()
		{
			var dialog = new OpenFileDialog();
			dialog.Filter = "All files|*.*";

			// Put initial directory in the same place as the current document, if there is one.
			if (ActiveDocument != null && !String.IsNullOrEmpty(ActiveDocument.FilePath))
			{
				dialog.InitialDirectory = Path.GetDirectoryName(ActiveDocument.FilePath);
			}
			else if (Workspace.Project != null)
			{
				dialog.InitialDirectory = Workspace.Project.BasePath;
			}

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
			ActiveWindow = document;
		}

		internal void CloseDocument(DocumentViewModel document)
		{
			document.Dispose();
			documents.Remove(document);
		}

		private void CloseAllDocuments()
		{
			foreach (var document in Documents)
			{
				document.Dispose();
			}
			documents.Clear();
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
				Workspace.RunScriptFile(ActiveDocument.FilePath);
			}
			else
			{
				await Workspace.RerunLastScript();
			}
		}

		public void RedrawViewports()
		{
			_renderWindow.Invalidate();
		}

		private ObservableCollection<DocumentViewModel> documents;
		public ReadOnlyObservableCollection<DocumentViewModel> Documents { get; }

		// Property that tracks the active window, document or otherwise.
		private object _activeWindow;
		public object ActiveWindow
		{
			get { return _activeWindow; }
			set { this.RaiseAndSetIfChanged(ref _activeWindow, value); }
		}

		// Property that tracks the currently active document.
		private ObservableAsPropertyHelper<DocumentViewModel> _activeDocument;
		public DocumentViewModel ActiveDocument => _activeDocument.Value;

		// Property source to use based on focus.
		private ObservableAsPropertyHelper<IPropertySource> _focusPropertySource;
		public IPropertySource FocusPropertySource => _focusPropertySource.Value;

		private ObservableAsPropertyHelper<ProjectViewModel> _projectViewModel;
		public ProjectViewModel ProjectViewModel => _projectViewModel.Value;

		private ObservableAsPropertyHelper<SceneViewModel> _sceneViewModel;
		public SceneViewModel SceneViewModel => _sceneViewModel.Value;

		public MenuBarViewModel MenuBar { get; }

		private ObservableAsPropertyHelper<IEnumerable<PropertyViewModel>> _properties;
		public IEnumerable<PropertyViewModel> Properties => _properties.Value;

		// Viewport view model that contains settings for the viewport (e.g. camera mode).
		private ViewportViewModel viewportViewModel = new ViewportViewModel();
		public ViewportViewModel ViewportViewModel => viewportViewModel;

		public Model.Workspace Workspace { get; }
		private readonly RenderWindow _renderWindow;

		// TODO: Make observable?
		public bool RealTimeMode
		{
			get { return _renderWindow != null && _renderWindow.RealTimeMode; }
			set { _renderWindow.RealTimeMode = value; }
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
							Workspace.OpenProject((string)param);
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
				param => RunActiveScript());

		// Command to save all (dirty) open documents.
		private NamedCommand saveAllCmd;
		public INamedCommand SaveAllCmd
			=> NamedCommand.LazyInit(ref saveAllCmd, "Save All",
				param => SaveAllDirty());

		#endregion
	}
}
