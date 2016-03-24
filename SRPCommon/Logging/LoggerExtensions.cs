using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Logging
{
	// Helper extension methods for ILogger.
	public static class LoggerExtensions
	{
		// Log a line ending in a carriage return to the logger.
		public static void LogLine(this ILogger logger, string line)
		{
			logger.Log(line + "\n");
		}

		// Log a line ending in a carriage return to the logger.
		public static void LogLine(this ILogger logger, string format, params object[] args)
		{
			logger.Log(string.Format(format + "\n", args));
		}

		// Create a stream that writes to the logger.
		public static Stream CreateStream(this ILogger logger) => new LoggerStream(logger);

		// Create a stream writer that writes to the logger.
		public static StreamWriter CreateStreamWriter(this ILogger logger) => new StreamWriter(new LoggerStream(logger), Encoding.UTF8);
	}
}
