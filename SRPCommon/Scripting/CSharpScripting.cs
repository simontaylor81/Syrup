using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using SRPCommon.Interfaces;
using SRPCommon.Logging;

namespace SRPCommon.Scripting
{
	class CSharpScripting
	{
		private ILoggerFactory _loggerFactory;
		private IWorkspace _workspace;

		public CSharpScripting(IWorkspace workspace, ILoggerFactory loggerFactory)
		{
			_workspace = workspace;
			_loggerFactory = loggerFactory;
		}

		public async Task<ICompiledScript> Compile(Script script)
		{
			var code = await ReadAllTextAsync(script.Filename);

			var options = ScriptOptions.Default
				.WithFilePath(script.Filename)
				.WithReferences(typeof(SRPScripting.IRenderInterface).Assembly);

			var compiled = CSharpScript.Create(code, options: options, globalsType: typeof(CSharpGlobals));
			var runner = compiled.CreateDelegate();
			return new CompiledCSharpScript(runner);
		}

		private async Task<string> ReadAllTextAsync(string filename)
		{
			using (var reader = new StreamReader(filename))
			{
				return await reader.ReadToEndAsync();
			}
		}
	}
}
