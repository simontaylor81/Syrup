using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using IronPython.Hosting;
using System.Reflection;
using System.Threading.Tasks;
using SRPCommon.Util;
using SRPScripting;
using System.Reactive;
using System.Reactive.Subjects;
using System.Diagnostics;
using SRPCommon.Interfaces;

namespace SRPCommon.Scripting
{
	public class Scripting
	{
		public IRenderInterface RenderInterface { get; set; }

		// Allow access to underlying Python engine for custom use (i.e. unit tests).
		public ScriptEngine PythonEngine => pythonEngine;

		private ScriptEngine pythonEngine;
		private SRPPlatformAdaptationLayer _pal;
		private bool bInProgress;

		// Events fired before and after script execution.
		public IObservable<Script> PreExecute => _preExecute;
		public IObservable<Exception> ExecutionComplete => _executionComplete;

		// Subjects for the above.
		private Subject<Script> _preExecute = new Subject<Script>();
		private Subject<Exception> _executionComplete = new Subject<Exception>();

		private const string _projectPathPrefix = "project:";

		public Scripting(IWorkspace workspace)
		{
			_pal = new SRPPlatformAdaptationLayer(workspace);

			// Create IronPython scripting engine.
			pythonEngine = CreatePythonEngine();

			// Load assemblies so the scripts can use them.
			pythonEngine.Runtime.LoadAssembly(typeof(string).Assembly);		// mscorlib.dll
			pythonEngine.Runtime.LoadAssembly(typeof(Uri).Assembly);		// System.dll
			pythonEngine.Runtime.LoadAssembly(typeof(SRPScripting.IRenderInterface).Assembly);
			pythonEngine.Runtime.LoadAssembly(typeof(System.Numerics.Vector3).Assembly);

			// Add stdlib dir to the search path.
			AddSearchPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "IronPythonLibs"));
			AddSearchPath(_projectPathPrefix);

			// Hook up log stream to the runtime.
			StreamWriter writer = OutputLogger.Instance.GetStreamWriter(LogCategory.Script);
			pythonEngine.Runtime.IO.SetOutput(writer.BaseStream, writer);
			pythonEngine.Runtime.IO.SetErrorOutput(writer.BaseStream, writer);
		}

		private ScriptEngine CreatePythonEngine()
		{
			var options = new Dictionary<string, object>
			{
				// Disable assembly loading hook. We don't need it, and it causes lots of
				// spurious exception thrown messages in the debug out when trying to load assemblies using
				// our 'project:' path prefix (assembly loading doesn't go through the PAL).
				// (Note that the exceptions are a problem as such, they just clutter up the log, hiding
				// important messages).
				{ "NoAssemblyResolveHook", true },
			};

			var runtimeSetup = Python.CreateRuntimeSetup(options);
			runtimeSetup.HostType = typeof(SRPScriptHost);
			runtimeSetup.HostArguments = new[] { _pal };
			var runtime = new ScriptRuntime(runtimeSetup);
			return Python.GetEngine(runtime);
		}

		public void AddSearchPath(string path)
		{
			pythonEngine.SetSearchPaths(
				pythonEngine.GetSearchPaths()
				.Concat(new[] { path })
				.ToList());
		}

		public async Task RunScript(Script script)
		{
			// Don't run if we're already running a script.
			if (!bInProgress)
			{
				bInProgress = true;

				_preExecute.OnNext(script);

				// Execute script on thread pool.
				try
				{
					await Task.Run(() =>
						{
							var source = pythonEngine.CreateScriptSourceFromFile(script.Filename);
							RunSource(source, script.GlobalVariables);
						});

					_executionComplete.OnNext(null);
				}
				catch (Exception ex)
				{
					_executionComplete.OnNext(ex);
					throw;
				}
				finally
				{
					bInProgress = false;
				}
			}
		}

		private void RunSource(ScriptSource source, IDictionary<string, object> globals)
		{
			// Clear any cached modules (user may have edited them).
			pythonEngine.GetSysModule().GetVariable("modules").Clear();

			// Create scope and set the render interface as a variable.
			Debug.Assert(RenderInterface != null);
			var pythonScope = pythonEngine.CreateScope();
			pythonScope.SetVariable("ri", RenderInterface);

			// Set any given global variables too.
			foreach (var global in globals)
			{
				pythonScope.SetVariable(global.Key, global.Value);
			}

			try
			{
				source.Execute(pythonScope);
			}
			catch (Exception ex)
			{
				var eo = pythonEngine.GetService<ExceptionOperations>();
				string error = eo.FormatException(ex);
				OutputLogger.Instance.LogLine(LogCategory.Script, "Script execution failed.");
				OutputLogger.Instance.LogLine(LogCategory.Script, error);

				throw;
			}
		}

		// Helper for formatting script errors.
		public string FormatScriptError(Exception ex)
		{
			var eo = pythonEngine.GetService<ExceptionOperations>();

			if (ex.InnerException != null)
			{
				string error = eo.FormatException(ex.InnerException);
				return $"{ex.Message}\n{error}\n";
			}
			else
			{
				string error = eo.FormatException(ex);
				return $"{error}\n";
			}
		}

		// Custom script host class that uses our custom PAL.
		private class SRPScriptHost : ScriptHost
		{
			private readonly PlatformAdaptationLayer _pal;
			public override PlatformAdaptationLayer PlatformAdaptationLayer => _pal;

			public SRPScriptHost(PlatformAdaptationLayer pal)
			{
				_pal = pal;
			}
		}

		// Custom PAL class to allow looking for imported modules in the workspace.
		private class SRPPlatformAdaptationLayer : PlatformAdaptationLayer
		{
			private IWorkspace _workspace;

			public SRPPlatformAdaptationLayer(IWorkspace workspace)
			{
				_workspace = workspace;
			}

			public override bool FileExists(string path)
			{
				if (_workspace != null && path.StartsWith(_projectPathPrefix, StringComparison.Ordinal))
				{
					return _workspace.FindProjectFile(path.Substring(_projectPathPrefix.Length)) != null;
				}
				return base.FileExists(path);
			}

			public override bool DirectoryExists(string path)
				=> (_workspace != null && path == _projectPathPrefix) || base.DirectoryExists(path);

			public override string[] GetFileSystemEntries(string path, string searchPattern, bool includeFiles, bool includeDirectories)
			{
				if (_workspace != null && path == _projectPathPrefix)
				{
					return new[] { _workspace.FindProjectFile(searchPattern) };
				}
				return base.GetFileSystemEntries(path, searchPattern, includeFiles, includeDirectories);
			}

			public override string GetDirectoryName(string path)
			{
				if (path.StartsWith(_projectPathPrefix, StringComparison.Ordinal))
				{
					return _projectPathPrefix;
				}
				return base.GetDirectoryName(path);
			}
		}
	}

	// Special exception class to throw when the user does something wrong.
	// Nothing special about it, but it allows us to ignore it explicitly in the debugger.
	// You should add "ShaderEditorApp.ScriptException" to the Debug->Exceptions window and disable break when user-unhandled.
	public class ScriptException : Exception
	{
		public ScriptException()
			: base()
		{}
		public ScriptException(string message)
			: base(message)
		{}
		public ScriptException(string message, Exception innerException)
			: base(message, innerException)
		{}
	}
}
