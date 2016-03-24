using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Logging
{
	// A logger that does nothing.
	public class NullLogger : ILogger
	{
		public void Clear() { }
		public void Log(string message) { }
	}
}
