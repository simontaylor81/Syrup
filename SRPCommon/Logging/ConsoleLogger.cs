using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Logging
{
	// A logger factory that creates console loggers.
	public class ConsoleLoggerFactory : ILoggerFactory
	{
		public ILogger CreateLogger(string category) => new ConsoleLogger(category);
	}

	// A logger that writes to the console.
	public class ConsoleLogger : ILogger
	{
		private readonly string _category;

		public ConsoleLogger(string category)
		{
			_category = category;
		}

		public void Log(string message)
		{
			Console.Write(_category.ToString() + ": " + message);
		}

		// Never clear the console.
		public void Clear() { }
	}
}
