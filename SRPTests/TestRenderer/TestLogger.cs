using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.Logging;
using Xunit.Abstractions;

namespace SRPTests.TestRenderer
{
	// A logger factory that creates test loggers.
	public class TestLoggerFactory : ILoggerFactory
	{
		private readonly ITestOutputHelper _output;

		public TestLoggerFactory(ITestOutputHelper output)
		{
			_output = output;
		}

		public ILogger CreateLogger(string category) => new TestLogger(_output, category);
	}

	// Class for logging output to ITestOutputHelper.
	class TestLogger : ILogger
	{
		private readonly ITestOutputHelper _output;
		private readonly string _category;

		public TestLogger(ITestOutputHelper output, string category)
		{
			_output = output;
			_category = category;
		}

		public void Log(string message)
		{
			// ITestOutputHelper only has WriteLine, so strip off any trailing carriage return.
			if (message[message.Length - 1] == '\n')
			{
				message = message.Substring(0, message.Length - 1);
			}

			_output.WriteLine(_category + ": " + message);
		}

		// Never clear test output.
		public void Clear() { }
	}
}
