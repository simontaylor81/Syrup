using System;
using System.Collections.Generic;
using System.Linq;
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
using SRPCommon.Interfaces;
using SRPCommon.Logging;

namespace SRPCommon.Scripting
{
	public class Scripting
	{
		private readonly PythonScripting _pythonScripting;

		public Scripting(IWorkspace workspace, ILoggerFactory loggerFactory)
		{
			_pythonScripting = new PythonScripting(workspace, loggerFactory);
		}

		public Task<ICompiledScript> Compile(Script script)
		{
			return _pythonScripting.Compile(script);
		}
	}
}
