using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Scripting
{
	// Special exception class to throw when the user does something wrong.
	// Nothing special about it, but it allows us to ignore it explicitly in the debugger.
	// You should add "ShaderEditorApp.ScriptException" to the Debug->Exceptions window and disable break when user-unhandled.
	public class ScriptException : Exception
	{
		public ScriptException()
			: base()
		{ }
		public ScriptException(string message)
			: base(message)
		{ }
		public ScriptException(string message, Exception innerException)
			: base(message, innerException)
		{ }
	}
}
