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
		private readonly ILoggerFactory _loggerFactory;
		private readonly WorkspaceReferenceResolver _referenceResolver;

		public CSharpScripting(IWorkspace workspace, ILoggerFactory loggerFactory)
		{
			_referenceResolver = new WorkspaceReferenceResolver(workspace);
			_loggerFactory = loggerFactory;
		}

		public async Task<ICompiledScript> Compile(Script script)
		{
			var code = await ReadAllTextAsync(script.Filename);

			var options = ScriptOptions.Default
				.WithFilePath(script.Filename)
				.WithReferences(
					typeof(SRPScripting.IRenderInterface).Assembly,
					typeof(System.Dynamic.DynamicObject).Assembly,
					typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly)
				// Add some common imports to avoid typing.
				.WithImports(
					"System",
					"System.Linq",
					"System.Collections.Generic",
					"System.Numerics",
					"SRPScripting")
				.WithSourceResolver(_referenceResolver);

			var compiled = CSharpScript.Create(code, options: options, globalsType: typeof(CSharpGlobals));
			var runner = compiled.CreateDelegate();
			return new CompiledCSharpScript(runner, script.TestParams);
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
