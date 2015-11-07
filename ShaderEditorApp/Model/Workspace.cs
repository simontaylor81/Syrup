using ReactiveUI;
using ShaderEditorApp.Projects;
using ShaderEditorApp.ViewModel;
using SRPCommon.Interfaces;
using SRPCommon.Scene;
using SRPCommon.Scripting;
using SRPCommon.Util;
using SRPRendering;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ShaderEditorApp.Model
{
	// Class for handling the workspace of the app (i.e. the central point of control).
	public class Workspace : ReactiveObject, IWorkspace
	{
		public Workspace(SlimDX.Direct3D11.Device d3dDevice)
		{
			// Create documents list, and wrap in a read-only wrapper.
			documents = new ObservableCollection<DocumentViewModel>();
			Documents = new ReadOnlyObservableCollection<DocumentViewModel>(documents);

			// Create classes that handle scripting.
			scripting = new Scripting(this);
			ScriptRenderControl = new ScriptRenderControl(this, d3dDevice, scripting);
			scripting.RenderInterface = ScriptRenderControl.ScriptInterface;
			ScriptRenderControl.RedrawRequired.Subscribe(_redrawRequired);
		}

		// Open the project with the given path.
		public void OpenProject(string filename)
		{
			Project = Project.LoadFromFile(filename);
		}

		// Create a new project.
		public void NewProject(string filename)
		{
			// Create a new project, and immediately save it to the given file.
			Project = Project.CreateNew(filename);
		}

		// TODO: Fix async void nastiness.
		internal async void RunScriptFile(string path)
		{
			var script = _scripts.GetOrAdd(path, () => new Script(path));
			await RunScript(script);
		}

		// Re-run the script that was last run (if there was one).
		public async Task RerunLastScript()
		{
			if (_lastRunScript != null)
			{
				await RunScript(_lastRunScript);
			}
		}

		private async Task RunScript(Script script)
		{
			_lastRunScript = script;

			// Asynchronously execute the script.
			await scripting.RunScript(script);

			_redrawRequired.OnNext(Unit.Default);
		}

		// Do we have a scene loaded currently?
		public bool HasCurrentScene => CurrentScene != null;

		// Load the scene with the given filename and set it as the current one.
		public void SetCurrentScene(string path)
		{
			// Attempt to load the scene.
			var newScene = Scene.LoadFromFile(path);
			if (newScene != null)
			{
				CurrentScene = newScene;
				ScriptRenderControl.Scene = CurrentScene;

				// Unsubscribe from previous scene.
				if (sceneChangeSubscription != null)
				{
					sceneChangeSubscription.Dispose();
				}

				// Redraw the scene when the scene changes.
				sceneChangeSubscription = CurrentScene.OnChanged.Subscribe(_redrawRequired);
			}
		}

		private Subject<Unit> _redrawRequired = new Subject<Unit>();
		public IObservable<Unit> RedrawRequired => _redrawRequired;

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
			return Path.Combine(Project.BasePath, path);
		}

		private ObservableCollection<DocumentViewModel> documents;
		public ReadOnlyObservableCollection<DocumentViewModel> Documents { get; }

		private Project _project;
		public Project Project
		{
			get { return _project; }
			private set
			{
				if (_project != value)
				{
					// Close existing documents when opening a new project.
					// TODO: Prompt to save.
					DisposableUtil.DisposeList(documents);

					_project = value;

					// Load default scene, if present.
					if (Project.DefaultScene != null)
					{
						SetCurrentScene(Project.DefaultScene.AbsolutePath);
					}

					// Run startup scripts.
					foreach (var script in value.StartupScripts)
					{
						RunScriptFile(script);
					}

					this.RaisePropertyChanged();
				}
			}
		}

		// Rendering/script related stuff.
		public ScriptRenderControl ScriptRenderControl { get; }
		private readonly Scripting scripting;
		//private readonly RenderWindow renderWindow;

		private Scene _currentScene;
		public Scene CurrentScene
		{
			get { return _currentScene; }
			set { this.RaiseAndSetIfChanged(ref _currentScene, value); }
		}

		private IDisposable sceneChangeSubscription;

		// Previously run scripts.
		private readonly Dictionary<string, Script> _scripts = new Dictionary<string, Script>();

		// Script file that was last run.
		private Script _lastRunScript;

		// List of documents that have been externally modified.
		private HashSet<DocumentViewModel> modifiedDocuments = new HashSet<DocumentViewModel>();

		// Is the application in the foreground?
		private bool isAppForeground = false;
	}
}
