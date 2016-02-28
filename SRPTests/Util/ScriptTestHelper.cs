using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Scripting;
using SRPCommon.Scripting;

namespace SRPTests.Util
{
	// Utils for test working with script.
	class ScriptTestHelper
	{
		private readonly Scripting _scripting;
		private readonly string[] _imports;

		public ScriptTestHelper(params string[] imports)
		{
			// Create Scripting object, which initialises the script engine.
			_scripting = new Scripting(null);
			_imports = imports;
		}

		// Helper for getting the value of some inline python code.
		public dynamic GetPythonValue(string expression)
		{
			// Create scope to evaluate the expression in.
			var scope = _scripting.PythonEngine.CreateScope();

			if (_imports != null && _imports.Length > 0)
			{
				// Import required imports into the scope.
				var importSrc = _scripting.PythonEngine.CreateScriptSourceFromString(
					string.Join("\n", _imports.Select(import => $"from {import} import *")),
					SourceCodeKind.Statements);
				importSrc.Execute(scope);
			}

			// Evaluate the expression in the scope.
			var source = _scripting.PythonEngine.CreateScriptSourceFromString(expression, SourceCodeKind.Expression);
			return source.Execute(scope);
		}
	}
}
