using System;
using System.Collections.Generic;
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

namespace SRPCommon.Scripting
{
	public class Scripting
	{
		public IRenderInterface RenderInterface { get; set; }

		private ScriptEngine pythonEngine;
		private bool bInProgress;

		// Events fired before and after script execution.
		public IObservable<Unit> PreExecute { get { return _preExecute; } }
		public IObservable<bool> ExecutionComplete { get { return _executionComplete; } }

		// Subjects for the above.
		private Subject<Unit> _preExecute = new Subject<Unit>();
		private Subject<bool> _executionComplete = new Subject<bool>();

		public Scripting()
		{
			// Create IronPython scripting engine.
			pythonEngine = Python.CreateEngine();

			// Load assemblies so the scripts can use them.
			pythonEngine.Runtime.LoadAssembly(typeof(String).Assembly);		// mscorlib.dll
			pythonEngine.Runtime.LoadAssembly(typeof(Uri).Assembly);		// System.dll
			pythonEngine.Runtime.LoadAssembly(typeof(SRPScripting.IRenderInterface).Assembly);

			// Add stdlib dir to the search path.
			var searchPaths = pythonEngine.GetSearchPaths();
			searchPaths.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "IronPythonLibs"));
			pythonEngine.SetSearchPaths(searchPaths);

			// Hook up log stream to the runtime.
			StreamWriter writer = OutputLogger.Instance.GetStreamWriter(LogCategory.Script);
			pythonEngine.Runtime.IO.SetOutput(writer.BaseStream, writer);
			pythonEngine.Runtime.IO.SetErrorOutput(writer.BaseStream, writer);

			ScriptHelper.Instance.Engine = pythonEngine;
		}

		public Task RunScriptFromFile(string filename)
		{
			return Execute(() => pythonEngine.CreateScriptSourceFromFile(filename));
		}

		public Task RunScript(string code)
		{
			return Execute(() => pythonEngine.CreateScriptSourceFromString(code, SourceCodeKind.Statements));
		}

		private async Task Execute(Func<ScriptSource> sourceFunc)
		{
			// Don't run if we're already running a script.
			if (!bInProgress)
			{
				bInProgress = true;

				// Reset the render controller.
//				renderControl.Reset();

				_preExecute.OnNext(Unit.Default);

				// Execute script on thread pool.
				bool bSuccess = await Task.Run(() => RunSource(sourceFunc()));

				//renderControl.ExecutionComplete(bSuccess);
				_executionComplete.OnNext(bSuccess);
				bInProgress = false;
			}
		}

		private bool RunSource(ScriptSource source)
		{
			// Create scope and set the render interface as a variable.
			Debug.Assert(RenderInterface != null);
			var pythonScope = pythonEngine.CreateScope();
			pythonScope.SetVariable("ri", RenderInterface);

			try
			{
				source.Execute(pythonScope);
				return true;
			}
			catch (Exception ex)
			{
				var eo = pythonEngine.GetService<ExceptionOperations>();
				string error = eo.FormatException(ex);
				OutputLogger.Instance.LogLine(LogCategory.Script, "Script execution failed.");
				OutputLogger.Instance.LogLine(LogCategory.Script, error);

				return false;
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
