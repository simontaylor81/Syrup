using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;
using SRPScripting;

namespace SRPCommon.Scripting
{
	class CompiledCSharpScript : ICompiledScript
	{
		private ScriptRunner<object> _runner;

		public CompiledCSharpScript(ScriptRunner<object> runner)
		{
			_runner = runner;
		}

		public Task ExecuteAsync(IRenderInterface renderInterface)
		{
			var globals = new CSharpGlobals
			{
				ri = renderInterface,
			};
			return _runner(globals);
		}

		public string FormatError(Exception ex)
		{
			// C# errors don't need an special processing.
			return ex.Message;
		}
	}
}
