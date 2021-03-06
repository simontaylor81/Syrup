﻿using System;
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
		private readonly ScriptRunner<object> _runner;
		private readonly IDictionary<string, object> _testParams;

		public CompiledCSharpScript(ScriptRunner<object> runner, IDictionary<string, object> testParams)
		{
			_runner = runner;
			_testParams = testParams;
		}

		public Task ExecuteAsync(IRenderInterface renderInterface)
		{
			var globals = new CSharpGlobals
			{
				ri = renderInterface,
				_testParams = _testParams,
			};

			// C# script execution is async in that any awaits will work, but it doesn't
			// actually run on a background thread, so any long running CPU work will still block the UI thread.
			// We don't want the user to be able to shoot themselves in the foot, so force background execution.
			return Task.Run(() => _runner(globals));
		}

		public string FormatError(Exception ex)
		{
			// C# errors don't need an special processing.
			return ex.Message;
		}
	}
}
