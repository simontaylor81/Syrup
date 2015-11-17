using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
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
using Splat;

namespace ShaderEditorApp.ViewModel
{
	// ViewModel for the application workspace, containing documents, docking windows, etc.
	public class WorkspaceViewModel : ReactiveObject
	{
		public WorkspaceViewModel(Workspace workspace, IUserPrompt userPrompt = null)
		{
			Workspace = workspace;
			_userPrompt = userPrompt ?? Locator.Current.GetService<IUserPrompt>();
			OpenDocumentSet = new OpenDocumentSetViewModel(this);

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

			// Status bar text, for now, prints whether the script is running.
			Workspace.IsScriptRunning
				.Select(isRunning => isRunning ? "Running script..." : "")
				.ToProperty(this, x => x.StatusBarText, out _statusBarText);

			// Create commands.
			{
				// Changing project whilst running scripts would be bad, so we disallow that
				// (plus the new project can run its own startup scripts).

				OpenProject = new CommandViewModel(
					"Open Project",
					CommandUtil.Create(param => OpenProjectPrompt(), Workspace.CanRunScript),
					menuHeader: "_Project",
					keyGesture: new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift));

				OpenProjectFile = CommandUtil.Create(param => Workspace.OpenProject((string)param), Workspace.CanRunScript);

				NewProject = new CommandViewModel(
					"New Project",
					CommandUtil.Create(param => NewProjectImpl(), Workspace.CanRunScript),
					menuHeader: "_Project",
					keyGesture: new KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Shift));

				OpenDocument = new CommandViewModel(
					"Open Document",
					CommandUtil.Create(param => OpenDocumentSet.OpenDocumentPrompt()),
					menuHeader: "_Document",
					keyGesture: new KeyGesture(Key.O, ModifierKeys.Control));

				OpenDocumentFile = ReactiveCommand.Create();
				OpenDocumentFile.Subscribe(param => OpenDocumentSet.OpenDocument((string)param, false));

				NewDocument = new CommandViewModel(
					"New Document",
					CommandUtil.Create(param => OpenDocumentSet.NewDocument()),
					menuHeader: "_Document",
					keyGesture: new KeyGesture(Key.N, ModifierKeys.Control));

				var hasActiveDocument = this.WhenAnyValue(x => x.OpenDocumentSet.ActiveDocument)
					.Select(doc => doc != null);

				CloseActiveDocument = new CommandViewModel(
					"Close",
					CommandUtil.Create(param => OpenDocumentSet.CloseDocument(OpenDocumentSet.ActiveDocument), hasActiveDocument),
					menuHeader: "_Close",
					keyGesture: new KeyGesture(Key.F4, ModifierKeys.Control));

				SaveActiveDocument = new CommandViewModel(
					"Save",
					CommandUtil.Create(param => OpenDocumentSet.ActiveDocument.Save(), hasActiveDocument),
					menuHeader: "_Save",
					keyGesture: new KeyGesture(Key.S, ModifierKeys.Control));

				SaveActiveDocumentAs = new CommandViewModel(
					"Save As",
					CommandUtil.Create(param => OpenDocumentSet.ActiveDocument.SaveAs(), hasActiveDocument),
					menuHeader: "Save _As");

				SaveAll = new CommandViewModel(
					"Save All",
					CommandUtil.Create(param => SaveAllDirty()),
					menuHeader: "Save A_ll",
					keyGesture: new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift));

				RunActiveScript = new CommandViewModel(
					"Run Current Script",
					ReactiveCommand.CreateAsyncTask(Workspace.CanRunScript, _ => RunActiveScriptImpl()),
					menuHeader: "_Run",
					keyGesture: new KeyGesture(Key.F5));

				Exit = ReactiveCommand.Create();
				ExitCommand = new CommandViewModel(
					"Exit",
					Exit,
					menuHeader: "E_xit",
					keyGesture: new KeyGesture(Key.F4, ModifierKeys.Alt));

				// Add commands with a key binding to the big list. Don't include Exit -- it's binding is part of Windows.
				KeyBoundCommands = new[]
				{
					OpenProject, NewProject, OpenDocument, NewDocument, CloseActiveDocument, SaveActiveDocument, SaveAll, RunActiveScript,
				};
			}

			// Create menu bar
			// Must be after the commands are created, for obvious reasons.
			MenuBar = new MenuBarViewModel(this);
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
		private void NewProjectImpl()
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

		private async Task RunActiveScriptImpl()
		{
			// Save all so we running the latest contents.
			if (!SaveAllDirty())
			{
				// Save failed (likely Save As cancelled). Abort.
				return;
			}

			if (OpenDocumentSet.ActiveDocument != null && OpenDocumentSet.ActiveDocument.IsScript)
			{
				await Workspace.RunScriptFile(OpenDocumentSet.ActiveDocument.FilePath);
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

		private ObservableAsPropertyHelper<string> _statusBarText;
		public string StatusBarText => _statusBarText.Value;

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

		// List of all command with a key binding.
		public IEnumerable<CommandViewModel> KeyBoundCommands { get; }

		// Command to prompt the user to select a project file to open, then open it.
		public CommandViewModel OpenProject { get; }

		// Command to open a specific project file, passed as a parameter.
		public ReactiveCommand<object> OpenProjectFile { get; }

		// Command to create a new project, closing the old one.
		public CommandViewModel NewProject { get; }

		// Command to open a document (shows the open file dialog).
		public CommandViewModel OpenDocument { get; }

		// Command to open a specific document file, passed as a parameter.
		public ReactiveCommand<object> OpenDocumentFile { get; }

		// Command to create a new document.
		public CommandViewModel NewDocument { get; }

		// Command to close the currently active document.
		public CommandViewModel CloseActiveDocument { get; }

		// Command to save the currently active document.
		public CommandViewModel SaveActiveDocument { get; }

		// Command to save the currently active document under a new filename.
		public CommandViewModel SaveActiveDocumentAs { get; }

		// Command to save all (dirty) open documents.
		public CommandViewModel SaveAll { get; }

		// Command to execute the currently active script document.
		public CommandViewModel RunActiveScript { get; }

		// Command to exit the application.
		// Command actually does nothing. It's up to the view to subscribe to it and do the actual exiting.
		public ReactiveCommand<object> Exit { get; }
		public CommandViewModel ExitCommand { get; }

		#endregion
	}
}
