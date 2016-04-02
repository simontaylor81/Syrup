using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Scripting.Hosting;
using SRPScripting;

namespace SRPCommon.Scripting
{
	// Implementation of ICompiledScript for python scripts.
	class CompiledPythonScript : ICompiledScript
	{
		private readonly CompiledCode _script;
		private readonly IEnumerable<KeyValuePair<string, object>> _globals;

		public CompiledPythonScript(CompiledCode script, IEnumerable<KeyValuePair<string, object>> globals)
		{
			_script = script;
			_globals = globals;
		}

		public Task ExecuteAsync(IRenderInterface renderInterface)
		{
			// Create scope and add global variables to it.
			var scope = _script.Engine.CreateScope();
			foreach (var global in _globals)
			{
				scope.SetVariable(global.Key, global.Value);
			}

			// Add the render interface.
			Trace.Assert(renderInterface != null);
			scope.SetVariable("ri", renderInterface);

			// Execute on background thread.
			return Task.Run(() => _script.Execute(scope));
		}

		public string FormatError(Exception ex) => PythonScripting.FormatError(ex, _script.Engine);
	}
}
