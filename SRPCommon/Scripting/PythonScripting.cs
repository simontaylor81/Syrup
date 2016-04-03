using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using SRPCommon.Interfaces;
using SRPCommon.Logging;

namespace SRPCommon.Scripting
{
	public class PythonScripting
	{
		// Allow access to underlying Python engine for custom use (i.e. unit tests).
		public ScriptEngine PythonEngine => pythonEngine;

		private ScriptEngine pythonEngine;
		private SRPPlatformAdaptationLayer _pal;

		private const string _projectPathPrefix = "project:";

		public PythonScripting(IWorkspace workspace, ILoggerFactory loggerFactory)
		{
			_pal = new SRPPlatformAdaptationLayer(workspace);

			// Create IronPython scripting engine.
			pythonEngine = CreatePythonEngine();

			// Load assemblies so the scripts can use them.
			pythonEngine.Runtime.LoadAssembly(typeof(string).Assembly);     // mscorlib.dll
			pythonEngine.Runtime.LoadAssembly(typeof(Uri).Assembly);        // System.dll
			pythonEngine.Runtime.LoadAssembly(typeof(SRPScripting.IRenderInterface).Assembly);
			pythonEngine.Runtime.LoadAssembly(typeof(System.Numerics.Vector3).Assembly);

			// Add stdlib dir to the search path.
			AddSearchPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "IronPythonLibs"));
			AddSearchPath(_projectPathPrefix);

			// Hook up log stream to the runtime.
			var writer = loggerFactory.CreateLogger("Script").CreateStreamWriter();
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

		private void AddSearchPath(string path)
		{
			pythonEngine.SetSearchPaths(
				pythonEngine.GetSearchPaths()
				.Concat(new[] { path })
				.ToList());
		}

		public Task<ICompiledScript> Compile(Script script)
		{
			// Poor man's async: run on background thread.
			return Task.Run(() =>
			{
				try
				{
					var source = pythonEngine.CreateScriptSourceFromFile(script.Filename);
					var compiled = source.Compile();
					return (ICompiledScript)new CompiledPythonScript(compiled, script.TestParams);
				}
				catch (Exception ex)
				{
					// Wrap exception to give friendly error message with file/line number.
					throw new ScriptException(FormatError(ex, pythonEngine), ex);
				}
			});
		}

		// Helper to format a script error.
		internal static string FormatError(Exception ex, ScriptEngine engine)
		{
			var eo = engine.GetService<ExceptionOperations>();
			var message = eo.FormatException(ex) + "\n";

			if (ex.InnerException != null)
			{
				message += $"  (Inner exception: {ex.InnerException.Message})\n";
			}

			return message;
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
}
