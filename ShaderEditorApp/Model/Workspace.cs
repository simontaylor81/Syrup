﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using ShaderEditorApp.Projects;
using SRPCommon.Interfaces;
using SRPCommon.Scene;
using SRPCommon.Scripting;
using SRPCommon.Util;
using SRPRendering;

namespace ShaderEditorApp.Model
{
	// Class for handling the workspace of the app (i.e. the central point of control).
	public class Workspace : ReactiveObject, IWorkspace, IDisposable
	{
		public Workspace(RenderDevice device)
		{
			// Create classes that handle scripting.
			scripting = new Scripting(this);
			Renderer = new SyrupRenderer(this, device, scripting);
			scripting.RenderInterface = Renderer.ScriptInterface;

			// Create script execution commands.
			RunScript = ReactiveCommand.CreateAsyncTask(param => RunScriptImpl_DoNotCallDirectly((Script)param));

			// Use CacheLatest so any subscriber will always get the most recent value.
			var canRunScript = RunScript.CanExecuteObservable.CacheLatest(true);
			canRunScript.Connect();
			CanRunScript = canRunScript;

			var isScriptRunning = RunScript.IsExecuting.CacheLatest(false);
			isScriptRunning.Connect();
			IsScriptRunning = isScriptRunning;

			// Redraw is required when we run a script, the scene changes, or the renderer says so.
			var sceneChanged = this.WhenAnyValue(x => x.CurrentScene)
				.Select(scene => scene != null ? scene.OnChanged : Observable.Empty<Unit>())
				.Switch();
			RedrawRequired = Observable.Merge(
				RunScript,
				sceneChanged,
				Renderer.RedrawRequired);
		}

		public void Dispose()
		{
			// Save settings on exit.
			UserSettings.Save();

			Renderer.Dispose();
		}

		// Open the project with the given path.
		public void OpenProject(string filename)
		{
			Project = Project.LoadFromFile(filename);
			UserSettings.RecentProjects.AddFile(filename);
			UserSettings.Save();
		}

		// Create a new project.
		public void NewProject(string filename)
		{
			// Create a new project, and immediately save it to the given file.
			Project = Project.CreateNew(filename);
			UserSettings.RecentProjects.AddFile(filename);
			UserSettings.Save();
		}

		public Task RunScriptFile(string path)
		{
			var script = _scripts.GetOrAdd(path, () => new Script(path));
			return RunScript.ExecuteAsyncTask(script);
		}

		// Re-run the script that was last run (if there was one).
		public async Task RerunLastScript()
		{
			if (_lastRunScript != null)
			{
				await RunScript.ExecuteAsyncTask(_lastRunScript);
			}
		}

		// Actually execute a script. Do not call this directly, use the RunScript command.
		private Task RunScriptImpl_DoNotCallDirectly(Script script)
		{
			_lastRunScript = script;

			// Asynchronously execute the script.
			return scripting.RunScript(script);
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
				Renderer.Scene = CurrentScene;
			}
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
			return Path.Combine(Project.BasePath, path);
		}

		private Project _project;
		public Project Project
		{
			get { return _project; }
			private set
			{
				if (_project != value)
				{
					_project = value;

					// Load default scene, if present.
					if (Project.DefaultScene != null)
					{
						SetCurrentScene(Project.DefaultScene.AbsolutePath);
					}

					// Run startup scripts.
					foreach (var script in value.StartupScripts)
					{
						// TODO: This is really bad, as you could have multiple scripts executing simultaneously!
						RunScriptFile(script);
					}

					this.RaisePropertyChanged();
				}
			}
		}

		// Rendering/script related stuff.
		public SyrupRenderer Renderer { get; }
		private readonly Scripting scripting;

		private Scene _currentScene;
		public Scene CurrentScene
		{
			get { return _currentScene; }
			set { this.RaiseAndSetIfChanged(ref _currentScene, value); }
		}

		public UserSettings UserSettings { get; } = new UserSettings();

		// Script execution command.
		// We use ReactiveCommand instead of just an async function as it tracks
		// when it's executing for us. Could do it by hand, but why bother?
		private ReactiveCommand<Unit> RunScript { get; }

		// Observables that indicate the state of script execution.
		public IObservable<bool> CanRunScript { get; }
		public IObservable<bool> IsScriptRunning { get; }

		// Observable that fires when the viewport should be redrawn.
		public IObservable<Unit> RedrawRequired { get; }

		// Previously run scripts.
		private readonly Dictionary<string, Script> _scripts = new Dictionary<string, Script>();

		// Script file that was last run.
		private Script _lastRunScript;
	}
}
