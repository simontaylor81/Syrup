using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using SRPCommon.Interfaces;
using SRPCommon.Logging;

namespace SRPCommon.Scripting
{
	public class CSharpScripting
	{
		private readonly ILoggerFactory _loggerFactory;
		private readonly WorkspaceReferenceResolver _referenceResolver;

		// Common usings to avoid typing.
		public static string[] Usings =
		{
			"System",
			"System.Linq",
			"System.Collections.Generic",
			"System.Numerics",
			"SRPScripting",
		};

		// References required to reproduce the script compilation environment.
		public static Assembly[] RequiredReferences =
		{
			typeof(object).Assembly,							// mscorlib.dll
			typeof(Uri).Assembly,								// System.
			typeof(Enumerable).Assembly,						// System.Core
			typeof(SRPScripting.IRenderInterface).Assembly,
			typeof(CSharpGlobals).Assembly,
			typeof(System.Numerics.Vector3).Assembly,
		};

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
					// We don't use RequiredReferences here as many of them are automatically added by the script framework.
					typeof(SRPScripting.IRenderInterface).Assembly,
					typeof(System.Numerics.Vector3).Assembly,
					typeof(System.Dynamic.DynamicObject).Assembly,
					typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly)
				.WithImports(Usings)
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
