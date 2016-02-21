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

		public ScriptTestHelper()
		{
			// Create Scripting object, which initialises the script engine.
			_scripting = new Scripting(null);
		}

		// Helper for getting the value of some inline python code.
		public dynamic GetPythonValue(string expression)
		{
			var source = _scripting.PythonEngine.CreateScriptSourceFromString(expression, SourceCodeKind.Expression);
			return source.Execute();
		}
	}
}
