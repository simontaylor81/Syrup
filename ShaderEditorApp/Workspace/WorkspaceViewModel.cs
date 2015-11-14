using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ShaderEditorApp.MVVMUtil;
using ShaderEditorApp.View;
using SRPCommon.UserProperties;
using SRPCommon.Util;
using ShaderEditorApp.ViewModel.Projects;
using ShaderEditorApp.ViewModel.Scene;
using System.Reactive;
using ReactiveUI;
using System.Reactive.Linq;
using ShaderEditorApp.Model;
using System.Threading.Tasks;
using ShaderEditorApp.Interfaces;

namespace ShaderEditorApp.ViewModel
{
	// ViewModel for the application workspace, containing documents, docking windows, etc.
	public class WorkspaceViewModel : ReactiveObject
	{
		public WorkspaceViewModel(Workspace workspace, IUserPrompt userPrompt)
		{
			Workspace = workspace;
			_userPrompt = userPrompt;
			OpenDocumentSet = new OpenDocumentSetViewModel(this);

			// Create menu bar
			MenuBar = new MenuBarViewModel(this);

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
				// Stream of properties from the focused window, or null if there is no focus property source.
				var focusProperties = this.WhenAnyValue(
						x => x.FocusPropertySource, x => x.FocusPropertySource.Properties,
						(source, props) => source != null ? props : null)
					.StartWithDefault();

				// Use focussed window if it's a property source, otherwise
				// fallback on the render properties (i.e. shader and user variables).
				var sourceProperties = focusProperties.CombineLatest(workspace.Renderer.PropertiesObservable,
					(focusProps, rendererProps) => focusProps ?? rendererProps);

				// Convert that into a stream of property view model lists.
				_properties = sourceProperties
					.Select(x => x
						// Create viewmodels for each property.
						.EmptyIfNull()
						.Select(prop => PropertyViewModelFactory.CreateViewModel(prop))
						.ToArray())
					.ToProperty(this, x => x.Properties);
			}

			// Project view model tracks the underlying project.
			_projectViewModel = workspace.WhenAnyValue(x => x.Project)
				.Select(project => project != null ? new ProjectViewModel(project, this) : null)
				.ToProperty(this, x => x.ProjectViewModel);

			// Scene view model tracks the underlying scene.
			_sceneViewModel = workspace.WhenAnyValue(x => x.CurrentScene)
				.Select(scene => scene != null ? new SceneViewModel(scene) : null)
				.ToProperty(this, x => x.SceneViewModel);
		}

		// Called when the app is about to exit. Returns true to allow exit to proceed, false to cancel.
		public async Task<bool> OnExit()
		{
			var unsavedFiles = OpenDocumentSet.Documents
				.Where(d => d.IsDirty)
				.Select(d => d.FilePath);

			if (Workspace.Project.IsDirty)
			{
				unsavedFiles = unsavedFiles.Concat(EnumerableEx.Return(Workspace.Project.FilePath));
			}

			if (unsavedFiles.Any())
			{
				var message = "The following files are unsaved:\n  "
					+ string.Join("\n  ", unsavedFiles)
					+ "\n\nWould you like to save them?";

				// Ask the user if they want to save.
				var result = await _userPrompt.ShowYesNoCancel(message);

				if (result == UserPromptResult.Yes)
				{
					// Save stuff. Allow exit if the save succeeded.
					return SaveAllDirty();
				}

				return result != UserPromptResult.Cancel;
			}

			return true;
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

		// TODO: Fix async void nastiness.
		private async void RunActiveScript()
		{
			// Save all so we running the latest contents.
			if (!SaveAllDirty())
			{
				// Save failed (likely Save As cancelled). Abort.
				return;
			}

			if (OpenDocumentSet.ActiveDocument != null && OpenDocumentSet.ActiveDocument.IsScript)
			{
				Workspace.RunScriptFile(OpenDocumentSet.ActiveDocument.FilePath);
			}
			else
			{
				await Workspace.RerunLastScript();
			}
		}

		// Saves all dirty documents, plus the project if it is dirty.
		private bool SaveAllDirty()
		{
			if (Workspace.Project.IsDirty)
			{
				Workspace.Project.Save();
			}

			return OpenDocumentSet.SaveAllDirty();
		}

		// Property that tracks the active window, document or otherwise.
		private object _activeWindow;
		public object ActiveWindow
		{
			get { return _activeWindow; }
			set { this.RaiseAndSetIfChanged(ref _activeWindow, value); }
		}

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

		public Workspace Workspace { get; }
		public OpenDocumentSetViewModel OpenDocumentSet { get; }

		private bool _realTimeMode;
		public bool RealTimeMode
		{
			get { return _realTimeMode; }
			set { this.RaiseAndSetIfChanged(ref _realTimeMode, value); }
		}

		private readonly IUserPrompt _userPrompt;

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
							OpenDocumentSet.OpenDocument((string)param, false);
						else
							OpenDocumentSet.OpenDocumentPrompt();
					});
			}
		}

		// Command to create a new document.
		private RelayCommand newDocumentCmd;
		public ICommand NewDocumentCmd => RelayCommand.LazyInit(ref newDocumentCmd, param => OpenDocumentSet.NewDocument());

		// Command to close the currently active document.
		private NamedCommand closeActiveDocumentCmd;
		public INamedCommand CloseActiveDocumentCmd
			=> NamedCommand.LazyInit(ref closeActiveDocumentCmd, "Close",
				param => OpenDocumentSet.CloseDocument(OpenDocumentSet.ActiveDocument),
				param => OpenDocumentSet.ActiveDocument != null);

		// Command to save the currently active document.
		private NamedCommand saveActiveDocumentCmd;
		public INamedCommand SaveActiveDocumentCmd
			=> NamedCommand.LazyInit(ref saveActiveDocumentCmd, "Save",
				param => OpenDocumentSet.ActiveDocument.Save(),
				param => OpenDocumentSet.ActiveDocument != null);

		// Command to save the currently active document under a new filename.
		private NamedCommand saveActiveDocumentAsCmd;
		public INamedCommand SaveActiveDocumentAsCmd
			=> NamedCommand.LazyInit(ref saveActiveDocumentAsCmd, "Save As",
				param => OpenDocumentSet.ActiveDocument.SaveAs(),
				param => OpenDocumentSet.ActiveDocument != null);

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

		// Command to exit the application.
		// Command actually does nothing. It's up to th new to subscribe to it and do the actual exiting.
		public IReactiveCommand<object> ExitCmd { get; } = ReactiveCommand.Create();

		#endregion
	}
}
